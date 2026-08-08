using System.Reflection;

namespace Reunion.ApiComparison;

internal static class ApiComparer
{
    private const string UnionInterfaceName = "System.Runtime.CompilerServices.IUnion";

    private static readonly HashSet<string> ExpectedUnionTypes = new(StringComparer.Ordinal)
    {
        "Reunion.Result",
        "Reunion.Result`1",
        "Reunion.Result`2",
        "Reunion.UnitResult`1",
        "Reunion.Option`1"
    };

    public static IReadOnlyList<string> Compare(Assembly net10, Assembly net11)
    {
        var errors = new List<string>();
        var net10Surface = net10.GetPublicSurface(
            excludeUnionProviders: true,
            compatibilityConversionProviders: ExpectedUnionTypes);
        var net11Surface = net11.GetPublicSurface(excludeUnionProviders: true);

        AddDifferences(errors, "net10-only public API", net10Surface.Except(net11Surface));
        AddDifferences(errors, "unexpected net11-only public API", net11Surface.Except(net10Surface));

        var net10Types = net10.GetExportedTypesByName();
        var net11Types = net11.GetExportedTypesByName();
        ValidateInterfaces(errors, net10Types, net11Types);
        ValidateAddedTypes(errors, net10Types, net11Types);
        ValidateCompatibilityConversions(errors, net10Types, net11Types);

        return errors;
    }

    public static IReadOnlyList<string> CompareExact(Assembly net10, Assembly net11)
    {
        var errors = new List<string>();
        var net10Surface = net10.GetPublicSurface(excludeUnionProviders: false);
        var net11Surface = net11.GetPublicSurface(excludeUnionProviders: false);

        AddDifferences(errors, "net10-only public API", net10Surface.Except(net11Surface));
        AddDifferences(errors, "net11-only public API", net11Surface.Except(net10Surface));
        return errors;
    }

    private static void ValidateCompatibilityConversions(
        ICollection<string> errors,
        IReadOnlyDictionary<string, Type> net10Types,
        IReadOnlyDictionary<string, Type> net11Types)
    {
        foreach (var typeName in ExpectedUnionTypes)
        {
            var net10Conversions = net10Types[typeName].GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Count(method => method.Name is "op_Implicit");
            var net11Conversions = net11Types[typeName].GetMethods(
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Count(method => method.Name is "op_Implicit");

            if (net10Conversions is not 2)
            {
                errors.Add(
                    $"{typeName} must expose exactly two net10 case compatibility conversions; "
                    + $"found {net10Conversions}.");
            }

            if (net11Conversions is not 0)
            {
                errors.Add(
                    $"{typeName} must rely on native net11 union conversions; "
                    + $"found {net11Conversions} metadata operators.");
            }
        }
    }

    private static void ValidateInterfaces(
        ICollection<string> errors,
        IReadOnlyDictionary<string, Type> net10Types,
        IReadOnlyDictionary<string, Type> net11Types)
    {
        foreach (var typeName in net10Types.Keys.Intersect(net11Types.Keys, StringComparer.Ordinal))
        {
            var oldInterfaces = net10Types[typeName].GetInterfaceNames();
            var newInterfaces = net11Types[typeName].GetInterfaceNames();
            var addedInterfaces = newInterfaces.Except(oldInterfaces).ToHashSet(StringComparer.Ordinal);
            var removedInterfaces = oldInterfaces.Except(newInterfaces).ToArray();

            if (removedInterfaces.Length is not 0)
            {
                errors.Add($"{typeName} lost interfaces: {string.Join(", ", removedInterfaces)}");
            }

            if (ExpectedUnionTypes.Contains(typeName))
            {
                var expectedProvider = typeName + "+IUnionMembers";
                if (!addedInterfaces.SetEquals([UnionInterfaceName, expectedProvider]))
                {
                    errors.Add($"{typeName} has unexpected added interfaces: {string.Join(", ", addedInterfaces)}");
                }
            }
            else if (addedInterfaces.Count is not 0)
            {
                errors.Add($"{typeName} unexpectedly gained interfaces: {string.Join(", ", addedInterfaces)}");
            }
        }
    }

    private static void ValidateAddedTypes(
        ICollection<string> errors,
        IReadOnlyDictionary<string, Type> net10Types,
        IReadOnlyDictionary<string, Type> net11Types)
    {
        var addedTypes = net11Types.Keys
            .Except(net10Types.Keys, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var expectedProviders = ExpectedUnionTypes
            .Select(name => name + "+IUnionMembers")
            .ToHashSet(StringComparer.Ordinal);

        if (!addedTypes.SetEquals(expectedProviders))
        {
            errors.Add($"Unexpected net11-only public types: {string.Join(", ", addedTypes)}");
        }

        foreach (var providerName in expectedProviders)
        {
            if (!net11Types.TryGetValue(providerName, out var provider))
            {
                errors.Add($"The expected union provider {providerName} is missing.");
            }
            else if (!provider.HasExpectedUnionProviderShape())
            {
                errors.Add(
                    $"{provider.FullName} does not have the expected two factories, "
                    + "two typed accessors, Value, and HasValue.");
            }
        }
    }

    private static void AddDifferences(
        ICollection<string> errors,
        string heading,
        IEnumerable<string> differences)
    {
        var materialized = differences.ToArray();
        if (materialized.Length is 0)
        {
            return;
        }

        errors.Add(
            heading
            + Environment.NewLine
            + string.Join(Environment.NewLine, materialized.Select(value => "  " + value)));
    }
}
