using System.Reflection;

namespace Reunion.Tests;

public sealed class ResultConversionSurfaceTests
{
    [Fact]
    public void NonGenericResultDeclaresNamedCaseConversions()
    {
        var sources = typeof(Result)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .Select(method => method.GetParameters().Single().ParameterType)
            .ToHashSet();

        Assert.Equal(2, sources.Count);
        Assert.Contains(typeof(Success), sources);
        Assert.Contains(typeof(Failure<string>), sources);
    }
}
