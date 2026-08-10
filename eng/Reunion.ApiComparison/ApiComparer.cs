using System.Reflection;

namespace Reunion.ApiComparison;

internal static class ApiComparer
{
    private const string UnionInterfaceName = "System.Runtime.CompilerServices.IUnion";

    private static readonly HashSet<string> KnownUnionTypes = new(StringComparer.Ordinal)
    {
        "Reunion.Result",
        "Reunion.Result`1",
        "Reunion.Result`2",
        "Reunion.UnitResult`1",
        "Reunion.Option`1",
        "Reunion.Validation.ValidationResult"
    };

    public static IReadOnlyList<string> Compare(Assembly net10, Assembly net11)
    {
        var errors = new List<string>();
        var net10Types = net10.GetExportedTypesByName();
        var net11Types = net11.GetExportedTypesByName();
        var expectedUnionTypes = KnownUnionTypes
            .Where(net11Types.ContainsKey)
            .ToHashSet(StringComparer.Ordinal);
        var compatibilityConversions = GetCompatibilityConversions(
            net10Types,
            net11Types,
            expectedUnionTypes);
        var net10Surface = net10.GetPublicSurface(
            excludeUnionProviders: true,
            excludedMembers: compatibilityConversions);
        var net11Surface = net11.GetPublicSurface(excludeUnionProviders: true);

        AddDifferences(errors, "net10-only public API", net10Surface.Except(net11Surface));
        AddDifferences(errors, "unexpected net11-only public API", net11Surface.Except(net10Surface));

        ValidateInterfaces(errors, net10Types, net11Types, expectedUnionTypes);
        ValidateAddedTypes(errors, net10Types, net11Types, expectedUnionTypes);
        ValidateCompatibilityConversions(errors, net10Types, net11Types, expectedUnionTypes);

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
        IReadOnlyDictionary<string, Type> net11Types,
        IReadOnlySet<string> expectedUnionTypes)
    {
        foreach (var typeName in expectedUnionTypes)
        {
            var net10Conversions = GetImplicitConversions(net10Types[typeName]);
            var net11ConversionSignatures = GetImplicitConversions(net11Types[typeName])
                .Select(method => method.ToString())
                .ToHashSet(StringComparer.Ordinal);
            var compatibilityConversions = net10Conversions
                .Where(method => !net11ConversionSignatures.Contains(method.ToString()))
                .ToArray();

            if (compatibilityConversions.Length is not 2)
            {
                errors.Add(
                    $"{typeName} must expose exactly two net10-only case compatibility conversions; "
                    + $"found {compatibilityConversions.Length}.");
                continue;
            }

            var providerName = typeName + "+IUnionMembers";
            var caseTypes = net11Types[providerName]
                .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(method => method.Name is "Create")
                .Select(method => method.GetParameters().Single().ParameterType.ToString())
                .ToHashSet(StringComparer.Ordinal);
            var compatibilitySources = compatibilityConversions
                .Select(method => method.GetParameters().Single().ParameterType.ToString())
                .ToHashSet(StringComparer.Ordinal);

            if (!compatibilitySources.SetEquals(caseTypes))
            {
                errors.Add(
                    $"{typeName} net10 compatibility conversions do not match its net11 union cases.");
            }
        }
    }

    private static HashSet<string> GetCompatibilityConversions(
        IReadOnlyDictionary<string, Type> net10Types,
        IReadOnlyDictionary<string, Type> net11Types,
        IReadOnlySet<string> expectedUnionTypes)
    {
        var compatibilityConversions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var typeName in expectedUnionTypes)
        {
            var net11ConversionSignatures = GetImplicitConversions(net11Types[typeName])
                .Select(method => method.ToString())
                .ToHashSet(StringComparer.Ordinal);

            foreach (var method in GetImplicitConversions(net10Types[typeName]))
            {
                if (!net11ConversionSignatures.Contains(method.ToString()))
                    compatibilityConversions.Add(method.ToPublicSurfaceKey());
            }
        }

        return compatibilityConversions;
    }

    private static MethodInfo[] GetImplicitConversions(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

    private static void ValidateInterfaces(
        ICollection<string> errors,
        IReadOnlyDictionary<string, Type> net10Types,
        IReadOnlyDictionary<string, Type> net11Types,
        IReadOnlySet<string> expectedUnionTypes)
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

            if (expectedUnionTypes.Contains(typeName))
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
        IReadOnlyDictionary<string, Type> net11Types,
        IReadOnlySet<string> expectedUnionTypes)
    {
        var addedTypes = net11Types.Keys
            .Except(net10Types.Keys, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var expectedProviders = expectedUnionTypes
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
