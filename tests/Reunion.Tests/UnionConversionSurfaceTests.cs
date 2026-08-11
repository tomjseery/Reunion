using System.Reflection;

namespace Reunion.Tests;

public sealed class UnionConversionSurfaceTests
{
    [Fact]
    public void NativeUnionTypesDeclareOnlyRawPayloadConversions()
    {
        AssertConversionSources(typeof(Result), typeof(string));
        AssertConversionSources(typeof(Result<int>), typeof(int), typeof(string));
        AssertConversionSources(typeof(Result<int, string>), typeof(int), typeof(string));
        AssertConversionSources(typeof(UnitResult<string>), typeof(string));
        AssertConversionSources(typeof(Option<int>), typeof(int));
    }

    private static void AssertConversionSources(Type target, params Type[] expectedSources)
    {
        var conversions = target
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

        Assert.Equal(expectedSources.Length, conversions.Length);
        Assert.All(conversions, conversion => Assert.Equal(target, conversion.ReturnType));
        Assert.True(
            expectedSources.ToHashSet().SetEquals(
                conversions.Select(conversion =>
                    conversion.GetParameters().Single().ParameterType)));
    }
}
