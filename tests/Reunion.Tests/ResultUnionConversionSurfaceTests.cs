using System.Reflection;

namespace Reunion.Tests;

public sealed class ResultUnionConversionSurfaceTests
{
    [Fact]
    public void NonGenericResultDeclaresNoOrdinaryConversions()
    {
        var conversions = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

        Assert.Empty(conversions);
    }
}
