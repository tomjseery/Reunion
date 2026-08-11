using System.Reflection;

namespace Reunion.ApiComparison;

internal static class ApiComparer
{
    private const string UnionInterfaceName = "System.Runtime.CompilerServices.IUnion";

    public static IReadOnlyList<string> Compare(Assembly downlevel, Assembly union)
    {
        var errors = new List<string>();
        var downlevelTypes = downlevel.GetExportedTypesByName();
        var unionTypes = union.GetExportedTypesByName();
        var unionTypeNames = unionTypes
            .Where(pair => pair.Value.GetInterfaceNames().Contains(UnionInterfaceName))
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);
        var compatibilityConversions = GetCompatibilityConversions(
            downlevelTypes,
            unionTypes,
            unionTypeNames);
        var unionSurface = union.GetUnionConsumerSurface();
        var downlevelSurface = downlevel.GetDownlevelConsumerSurface(compatibilityConversions);

        AddDifferences(errors, "missing downlevel public API", unionSurface.Except(downlevelSurface));
        AddDifferences(errors, "unexpected downlevel public API", downlevelSurface.Except(unionSurface));

        ValidateInterfaces(errors, downlevelTypes, unionTypes, unionTypeNames);
        ValidateAddedTypes(errors, downlevelTypes, unionTypes, unionTypeNames);
        ValidateCaseConversions(errors, downlevelTypes, unionTypes, unionTypeNames);

        return errors;
    }

    public static IReadOnlyList<string> CompareExact(Assembly downlevel, Assembly union)
    {
        var errors = new List<string>();
        var downlevelSurface = downlevel.GetPublicSurface();
        var unionSurface = union.GetPublicSurface();

        AddDifferences(errors, "missing downlevel public API", unionSurface.Except(downlevelSurface));
        AddDifferences(errors, "unexpected downlevel public API", downlevelSurface.Except(unionSurface));
        return errors;
    }

    private static void ValidateCaseConversions(
        ICollection<string> errors,
        IReadOnlyDictionary<string, Type> downlevelTypes,
        IReadOnlyDictionary<string, Type> unionTypes,
        IReadOnlySet<string> unionTypeNames)
    {
        foreach (var typeName in unionTypeNames)
        {
            if (!downlevelTypes.TryGetValue(typeName, out var downlevelType))
                continue;

            var providerName = typeName + "+IUnionMembers";
            if (!unionTypes.TryGetValue(providerName, out var provider))
                continue;

            if (!provider.TryGetUnionCaseTypeNames(out var caseTypes))
                continue;

            var downlevelConversions = GetCaseConversions(downlevelType, caseTypes);
            if (!TryGetConversionSourceTypeNames(
                    downlevelType,
                    downlevelConversions,
                    out var downlevelSources)
                || !downlevelSources.SetEquals(caseTypes))
            {
                errors.Add($"{typeName} downlevel named-case conversions do not match its union cases.");
            }

            var unionType = unionTypes[typeName];
            var unionConversions = GetCaseConversions(unionType, caseTypes);
            if (!TryGetConversionSourceTypeNames(unionType, unionConversions, out var unionSources)
                || (unionSources.Count is not 0 && !unionSources.SetEquals(caseTypes)))
            {
                errors.Add($"{typeName} union named-case conversions do not match its union cases.");
            }
        }
    }

    private static MethodInfo[] GetCaseConversions(
        Type type,
        IReadOnlySet<string> caseTypes) =>
        GetImplicitConversions(type)
            .Where(method => method.GetParameters() is [{ ParameterType: var source }]
                && caseTypes.Contains(source.ToString()))
            .ToArray();

    private static HashSet<string> GetCompatibilityConversions(
        IReadOnlyDictionary<string, Type> downlevelTypes,
        IReadOnlyDictionary<string, Type> unionTypes,
        IReadOnlySet<string> unionTypeNames)
    {
        var compatibilityConversions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var typeName in unionTypeNames)
        {
            if (!downlevelTypes.TryGetValue(typeName, out var downlevelType))
                continue;

            var unionConversionSignatures = GetImplicitConversions(unionTypes[typeName])
                .Select(method => method.ToString())
                .ToHashSet(StringComparer.Ordinal);

            foreach (var method in GetImplicitConversions(downlevelType))
            {
                if (!unionConversionSignatures.Contains(method.ToString()))
                    compatibilityConversions.Add(method.ToPublicSurfaceKey());
            }
        }

        return compatibilityConversions;
    }

    private static MethodInfo[] GetImplicitConversions(Type type) =>
        type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

    private static bool TryGetConversionSourceTypeNames(
        Type targetType,
        IEnumerable<MethodInfo> conversions,
        out HashSet<string> sourceTypeNames)
    {
        sourceTypeNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in conversions)
        {
            var parameters = method.GetParameters();
            if (!method.IsStatic
                || !method.IsSpecialName
                || method.ReturnType != targetType
                || parameters.Length is not 1
                || parameters[0].IsOut
                || parameters[0].ParameterType.IsByRef)
            {
                return false;
            }

            sourceTypeNames.Add(parameters[0].ParameterType.ToString());
        }

        return true;
    }

    private static void ValidateInterfaces(
        ICollection<string> errors,
        IReadOnlyDictionary<string, Type> downlevelTypes,
        IReadOnlyDictionary<string, Type> unionTypes,
        IReadOnlySet<string> unionTypeNames)
    {
        foreach (var typeName in downlevelTypes.Keys.Intersect(unionTypes.Keys, StringComparer.Ordinal))
        {
            var downlevelInterfaces = downlevelTypes[typeName].GetInterfaceNames();
            var unionInterfaces = unionTypes[typeName].GetInterfaceNames();
            var unionOnlyInterfaces = unionInterfaces
                .Except(downlevelInterfaces)
                .ToHashSet(StringComparer.Ordinal);
            var downlevelOnlyInterfaces = downlevelInterfaces.Except(unionInterfaces).ToArray();

            if (downlevelOnlyInterfaces.Length is not 0)
            {
                errors.Add(
                    $"{typeName} has downlevel-only interfaces: "
                    + string.Join(", ", downlevelOnlyInterfaces));
            }

            if (unionTypeNames.Contains(typeName))
            {
                var expectedProvider = typeName + "+IUnionMembers";
                if (!unionOnlyInterfaces.SetEquals([UnionInterfaceName, expectedProvider]))
                {
                    errors.Add(
                        $"{typeName} has unexpected union-only interfaces: "
                        + string.Join(", ", unionOnlyInterfaces));
                }
            }
            else if (unionOnlyInterfaces.Count is not 0)
            {
                errors.Add(
                    $"{typeName} unexpectedly has union-only interfaces: "
                    + string.Join(", ", unionOnlyInterfaces));
            }
        }
    }

    private static void ValidateAddedTypes(
        ICollection<string> errors,
        IReadOnlyDictionary<string, Type> downlevelTypes,
        IReadOnlyDictionary<string, Type> unionTypes,
        IReadOnlySet<string> unionTypeNames)
    {
        var unionOnlyTypes = unionTypes.Keys
            .Except(downlevelTypes.Keys, StringComparer.Ordinal)
            .ToHashSet(StringComparer.Ordinal);
        var expectedProviders = unionTypeNames
            .Select(name => name + "+IUnionMembers")
            .ToHashSet(StringComparer.Ordinal);

        if (!unionOnlyTypes.SetEquals(expectedProviders))
        {
            errors.Add($"Unexpected union-only public types: {string.Join(", ", unionOnlyTypes)}");
        }

        foreach (var providerName in expectedProviders)
        {
            if (!unionTypes.TryGetValue(providerName, out var provider))
            {
                errors.Add($"The expected union provider {providerName} is missing.");
            }
            else if (!provider.TryGetUnionCaseTypeNames(out _))
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
