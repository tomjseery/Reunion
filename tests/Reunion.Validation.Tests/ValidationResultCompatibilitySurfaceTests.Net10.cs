using System.Reflection;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultCompatibilitySurfaceTests
{
    [Fact]
    public void PublicConversionSurfaceContainsOnlyNamedCases()
    {
        var conversions = typeof(ValidationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

        Assert.Equal([typeof(Invalid), typeof(Valid)], conversions
            .Select(method => method.GetParameters().Single().ParameterType)
            .OrderBy(candidate => candidate.Name));
    }
}
