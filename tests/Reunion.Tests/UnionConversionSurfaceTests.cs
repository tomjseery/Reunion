using System.Reflection;

namespace Reunion.Tests;

public sealed class UnionConversionSurfaceTests
{
    [Theory]
    [InlineData(typeof(Result))]
    [InlineData(typeof(Result<int>))]
    [InlineData(typeof(Result<int, string>))]
    [InlineData(typeof(UnitResult<string>))]
    [InlineData(typeof(Option<int>))]
    public void NativeUnionTypesDeclareNoOrdinaryImplicitConversions(Type type)
    {
        var conversions = type
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit");

        Assert.Empty(conversions);
    }
}
