using System.Reflection;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultUnionConversionSurfaceTests
{
    [Fact]
    public void NativeUnionTypeDeclaresOnlyLosslessUnitResultConversion()
    {
        var conversions = typeof(ValidationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit");

        var conversion = Assert.Single(conversions);
        Assert.Equal(typeof(ValidationResult), conversion.GetParameters().Single().ParameterType);
        Assert.Equal(typeof(UnitResult<Reunion.Errors.ValidationErrors>), conversion.ReturnType);
    }
}
