using System.Reflection;

namespace Reunion.ApiComparison;

internal static class ReflectionExtensions
{
    public static SortedSet<string> GetPublicSurface(
        this Assembly assembly,
        bool excludeUnionProviders,
        IReadOnlySet<string>? compatibilityConversionProviders = null)
    {
        var surface = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var type in assembly.GetExportedTypes())
        {
            if (excludeUnionProviders && type.IsUnionProvider())
            {
                continue;
            }

            surface.Add($"type:{type.FullName}");
            foreach (var member in type.GetMembers(
                         BindingFlags.Public
                         | BindingFlags.Instance
                         | BindingFlags.Static
                         | BindingFlags.DeclaredOnly))
            {
                if (excludeUnionProviders && member is Type nestedType && nestedType.IsUnionProvider())
                {
                    continue;
                }

                if (compatibilityConversionProviders?.Contains(type.FullName!) is true
                    && member is MethodInfo { Name: "op_Implicit" })
                {
                    continue;
                }

                surface.Add($"member:{type.FullName}:{member.MemberType}:{member}");
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

    public static bool HasExpectedUnionProviderShape(this Type provider)
    {
        if (!provider.IsInterface || !provider.IsNestedPublic)
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

        return methods.Count(method => method.Name is "Create" && method.IsStatic) is 2
            && methods.Count(method => method.Name is "TryGetValue" && !method.IsStatic) is 2
            && properties.Select(property => property.Name)
                .ToHashSet(StringComparer.Ordinal)
                .SetEquals(["Value", "HasValue"]);
    }
}
