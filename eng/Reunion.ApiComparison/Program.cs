using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: Reunion.ApiComparison <net10 Reunion.dll> <net11 Reunion.dll>");
    return 2;
}

var net10Path = Path.GetFullPath(args[0]);
var net11Path = Path.GetFullPath(args[1]);

using var net10 = LoadedAssembly.Open("Reunion-net10", net10Path);
using var net11 = LoadedAssembly.Open("Reunion-net11", net11Path);

var expectedUnionTypes = new HashSet<string>(StringComparer.Ordinal)
{
    "Reunion.Result",
    "Reunion.Result`1",
    "Reunion.Result`2",
    "Reunion.UnitResult`1",
    "Reunion.Option`1"
};

var net10Surface = GetPublicSurface(net10.Assembly, excludeUnionProviders: true);
var net11Surface = GetPublicSurface(net11.Assembly, excludeUnionProviders: true);

ReportDifferences("net10-only public API", net10Surface.Except(net11Surface));
ReportDifferences("unexpected net11-only public API", net11Surface.Except(net10Surface));

var net10Types = GetTypes(net10.Assembly);
var net11Types = GetTypes(net11.Assembly);

foreach (var typeName in net10Types.Keys.Intersect(net11Types.Keys, StringComparer.Ordinal))
{
    var oldInterfaces = InterfaceNames(net10Types[typeName]);
    var newInterfaces = InterfaceNames(net11Types[typeName]);
    var addedInterfaces = newInterfaces.Except(oldInterfaces).ToHashSet(StringComparer.Ordinal);
    var removedInterfaces = oldInterfaces.Except(newInterfaces).ToArray();

    if (removedInterfaces.Length != 0)
    {
        Fail($"{typeName} lost interfaces: {string.Join(", ", removedInterfaces)}");
    }

    if (expectedUnionTypes.Contains(typeName))
    {
        var expectedProvider = typeName + "+IUnionMembers";
        if (!addedInterfaces.SetEquals([typeof(IUnion).FullName!, expectedProvider]))
        {
            Fail($"{typeName} has unexpected added interfaces: {string.Join(", ", addedInterfaces)}");
        }
    }
    else if (addedInterfaces.Count != 0)
    {
        Fail($"{typeName} unexpectedly gained interfaces: {string.Join(", ", addedInterfaces)}");
    }
}

var addedTypes = net11Types.Keys.Except(net10Types.Keys, StringComparer.Ordinal).ToHashSet(StringComparer.Ordinal);
var expectedProviders = expectedUnionTypes.Select(name => name + "+IUnionMembers").ToHashSet(StringComparer.Ordinal);
if (!addedTypes.SetEquals(expectedProviders))
{
    Fail($"Unexpected net11-only public types: {string.Join(", ", addedTypes)}");
}

foreach (var providerName in expectedProviders)
{
    ValidateProvider(net11Types[providerName]);
}

if (Environment.ExitCode != 0)
{
    return Environment.ExitCode;
}

Console.WriteLine("Public APIs match; only IUnion and the five validated IUnionMembers providers differ.");
return 0;

static SortedSet<string> GetPublicSurface(Assembly assembly, bool excludeUnionProviders)
{
    var surface = new SortedSet<string>(StringComparer.Ordinal);
    foreach (var type in assembly.GetExportedTypes())
    {
        if (excludeUnionProviders && IsUnionProvider(type))
        {
            continue;
        }

        surface.Add($"type:{type.FullName}");
        foreach (var member in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            if (excludeUnionProviders && member is Type nestedType && IsUnionProvider(nestedType))
            {
                continue;
            }

            surface.Add($"member:{type.FullName}:{member.MemberType}:{member}");
        }
    }

    return surface;
}

static Dictionary<string, Type> GetTypes(Assembly assembly) => assembly.GetExportedTypes()
    .ToDictionary(type => type.FullName!, StringComparer.Ordinal);

static HashSet<string> InterfaceNames(Type type) => type.GetInterfaces()
    .Select(candidate => candidate.IsGenericType ? candidate.GetGenericTypeDefinition().FullName! : candidate.FullName!)
    .ToHashSet(StringComparer.Ordinal);

static bool IsUnionProvider(Type type) => type.Name == "IUnionMembers" && type.IsNestedPublic;

static void ValidateProvider(Type provider)
{
    if (!provider.IsInterface || !provider.IsNestedPublic)
    {
        Fail($"{provider.FullName} is not a public nested interface.");
    }

    var methods = provider.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
    var properties = provider.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

    if (methods.Count(method => method.Name == "Create" && method.IsStatic) != 2
        || methods.Count(method => method.Name == "TryGetValue" && !method.IsStatic) != 2
        || properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal).SetEquals(["Value", "HasValue"]) is false)
    {
        Fail($"{provider.FullName} does not have the expected two factories, two typed accessors, Value, and HasValue.");
    }
}

static void ReportDifferences(string heading, IEnumerable<string> differences)
{
    var materialized = differences.ToArray();
    if (materialized.Length == 0)
    {
        return;
    }

    Fail(heading + Environment.NewLine + string.Join(Environment.NewLine, materialized.Select(value => "  " + value)));
}

static void Fail(string message)
{
    Console.Error.WriteLine(message);
    Environment.ExitCode = 1;
}

file sealed class LoadedAssembly : IDisposable
{
    private readonly AssemblyLoadContext context;

    private LoadedAssembly(AssemblyLoadContext context, Assembly assembly)
    {
        this.context = context;
        this.Assembly = assembly;
    }

    public Assembly Assembly { get; }

    public static LoadedAssembly Open(string name, string path)
    {
        var context = new AssemblyLoadContext(name, isCollectible: true);
        return new LoadedAssembly(context, context.LoadFromAssemblyPath(path));
    }

    public void Dispose() => this.context.Unload();
}
