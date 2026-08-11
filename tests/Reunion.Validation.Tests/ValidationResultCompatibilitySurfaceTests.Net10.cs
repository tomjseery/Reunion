using System.Reflection;
using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultCompatibilitySurfaceTests
{
    [Fact]
    public void PublicConversionSurfaceContainsRawPayloadAndNamedCases()
    {
        var conversions = typeof(ValidationResult)
            .GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.Name is "op_Implicit")
            .ToArray();

        Assert.Equal([typeof(Invalid), typeof(Valid), typeof(ValidationErrors)], conversions
            .Select(method => method.GetParameters().Single().ParameterType)
            .OrderBy(candidate => candidate.Name));
    }
}
