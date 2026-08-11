using System.Reflection;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultUnionConversionSurfaceTests
{
    [Fact]
    public void NativeUnionTypeDeclaresNoOrdinaryImplicitConversions()
    {
        var conversions = typeof(ValidationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit");

        Assert.Empty(conversions);
    }
}
