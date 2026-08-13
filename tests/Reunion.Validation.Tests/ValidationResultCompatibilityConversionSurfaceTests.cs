#if !NET11_0_OR_GREATER
using System.Reflection;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultCompatibilityConversionSurfaceTests
{
    [Fact]
    public void PublicConversionSurfaceContainsNamedCasesAndLosslessUnitResult()
    {
        var conversions = typeof(ValidationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

        Assert.Equal([typeof(Invalid), typeof(Valid), typeof(ValidationResult)], conversions
            .Select(method => method.GetParameters().Single().ParameterType)
            .OrderBy(candidate => candidate.Name));
    }
}
#endif
