using System.Reflection;

namespace Reunion.ApiComparison;

internal static class ReflectionExtensions
{
    public static SortedSet<string> GetPublicSurface(this Assembly assembly) =>
        assembly.BuildPublicSurface(static _ => true, static _ => true);

    public static SortedSet<string> GetUnionConsumerSurface(this Assembly assembly) =>
        assembly.BuildPublicSurface(
            static type => !type.IsUnionProvider(),
            static member => member is not Type nestedType || !nestedType.IsUnionProvider());

    public static SortedSet<string> GetDownlevelConsumerSurface(
        this Assembly assembly,
        IReadOnlySet<string> compatibilityConversions) =>
        assembly.BuildPublicSurface(
            static _ => true,
            member => !compatibilityConversions.Contains(member.ToPublicSurfaceKey()));

    private static SortedSet<string> BuildPublicSurface(
        this Assembly assembly,
        Func<Type, bool> includeType,
        Func<MemberInfo, bool> includeMember)
    {
        var surface = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in assembly.GetExportedTypes())
        {
            if (!includeType(type))
                continue;

            surface.Add($"type:{type.FullName}");
            foreach (var member in type.GetMembers(
                         BindingFlags.Public
                         | BindingFlags.Instance
                         | BindingFlags.Static
                         | BindingFlags.DeclaredOnly))
            {
                if (!includeMember(member))
                    continue;

                surface.Add(member.ToPublicSurfaceKey());
            }
        }

        return surface;
    }

    public static Dictionary<string, Type> GetExportedTypesByName(this Assembly assembly) =>
        assembly.GetExportedTypes().ToDictionary(type => type.FullName!, StringComparer.Ordinal);

    public static HashSet<string> GetInterfaceNames(this Type type) =>
        type.GetInterfaces()
            .Select(candidate => candidate.IsGenericType
                ? candidate.GetGenericTypeDefinition().FullName!
                : candidate.FullName!)
            .ToHashSet(StringComparer.Ordinal);

    public static bool IsUnionProvider(this Type type) =>
        type.Name is "IUnionMembers" && type.IsNestedPublic;

    public static string ToPublicSurfaceKey(this MemberInfo member) =>
        $"member:{member.DeclaringType!.FullName}:{member.MemberType}:{member}";

    public static bool TryGetUnionCaseTypeNames(
        this Type provider,
        out HashSet<string> caseTypeNames)
    {
        caseTypeNames = new HashSet<string>(StringComparer.Ordinal);
        if (!provider.IsInterface || !provider.IsNestedPublic || provider.DeclaringType is null)
        {
            return false;
        }

        var methods = provider.GetMethods(
            BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly);
        var properties = provider.GetProperties(
            BindingFlags.Public
            | BindingFlags.Instance
            | BindingFlags.Static
            | BindingFlags.DeclaredOnly);
        var createMethods = methods.Where(method => method.Name is "Create").ToArray();
        var tryGetMethods = methods.Where(method => method.Name is "TryGetValue").ToArray();

        if (createMethods.Length is not 2
            || tryGetMethods.Length is not 2
            || properties.Length is not 2)
        {
            return false;
        }

        foreach (var method in createMethods)
        {
            var parameters = method.GetParameters();
            if (!method.IsStatic
                || method.IsGenericMethod
                || !HasEquivalentSignatureType(method.ReturnType, provider.DeclaringType)
                || parameters.Length is not 1
                || parameters[0].IsOut
                || parameters[0].ParameterType.IsByRef)
            {
                return false;
            }

            caseTypeNames.Add(parameters[0].ParameterType.ToString());
        }

        if (caseTypeNames.Count is not 2)
            return false;

        var accessorCaseTypes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var method in tryGetMethods)
        {
            var parameters = method.GetParameters();
            if (method.IsStatic
                || method.IsGenericMethod
                || method.ReturnType != typeof(bool)
                || parameters.Length is not 1
                || !parameters[0].IsOut
                || !parameters[0].ParameterType.IsByRef)
            {
                return false;
            }

            accessorCaseTypes.Add(parameters[0].ParameterType.GetElementType()!.ToString());
        }

        if (!accessorCaseTypes.SetEquals(caseTypeNames))
            return false;

        var valueProperties = properties.Where(property => property.Name is "Value").ToArray();
        var hasValueProperties = properties.Where(property => property.Name is "HasValue").ToArray();
        return valueProperties.Length is 1
            && hasValueProperties.Length is 1
            && IsExpectedProperty(valueProperties[0], typeof(object))
            && IsExpectedProperty(hasValueProperties[0], typeof(bool));
    }

    private static bool IsExpectedProperty(PropertyInfo? property, Type propertyType) =>
        property is not null
        && property.PropertyType == propertyType
        && property.GetMethod is { IsPublic: true, IsStatic: false }
        && property.SetMethod is null
        && property.GetIndexParameters().Length is 0;

    private static bool HasEquivalentSignatureType(Type actual, Type expected)
    {
        if (actual == expected)
            return true;

        if (actual.IsGenericParameter && expected.IsGenericParameter)
            return actual.GenericParameterPosition == expected.GenericParameterPosition;

        if (!actual.IsGenericType
            || !expected.IsGenericType
            || actual.GetGenericTypeDefinition() != expected.GetGenericTypeDefinition())
        {
            return false;
        }

        var actualArguments = actual.GetGenericArguments();
        var expectedArguments = expected.GetGenericArguments();
        return actualArguments.Length == expectedArguments.Length
            && actualArguments
                .Zip(expectedArguments)
                .All(pair => HasEquivalentSignatureType(pair.First, pair.Second));
    }
}
