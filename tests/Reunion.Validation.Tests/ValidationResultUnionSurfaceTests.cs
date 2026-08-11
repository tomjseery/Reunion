using System.Reflection;
using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultUnionSurfaceTests
{
    [Fact]
    public void NativeUnionTypeDeclaresOnlyRawPayloadConversion()
    {
        var conversions = typeof(ValidationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit");

        Assert.Equal([typeof(ValidationErrors)], conversions
            .Select(method => method.GetParameters().Single().ParameterType));
    }
}
