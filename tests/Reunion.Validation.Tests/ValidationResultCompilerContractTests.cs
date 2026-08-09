#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;
using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultCompilerContractTests
{
    [Fact]
    public void ExhaustiveSwitchAndNativeConversions_CoverBothCases()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        ValidationResult valid = new Valid();
        ValidationResult invalid = new Invalid(errors);

        Assert.Equal("valid", Match(valid));
        Assert.Equal("Required.", Match(invalid));
    }

    [Fact]
    public void PositionalInvalidPattern_ExposesErrors()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        ValidationResult validation = new Invalid(errors);

        Assert.Same(errors, MatchErrors(validation));
    }

    [Fact]
    public void DefaultUnionValue_IsNullAndHasNoCase()
    {
        var validation = default(ValidationResult);
        var members = (ValidationResult.IUnionMembers)validation;

        Assert.Null(((IUnion)validation).Value);
        Assert.Null(members.Value);
        Assert.False(members.HasValue);
        Assert.False(members.TryGetValue(out Valid _));
        Assert.False(members.TryGetValue(out Invalid _));
        Assert.Equal("uninitialized", MatchDefault(validation));
    }

    [Fact]
    public void NativeMatching_AddsNoBoxingAllocation()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        ValidationResult valid = new Valid();
        ValidationResult invalid = new Invalid(errors);
        _ = MeasureMatching(valid, invalid);

        Assert.Equal(0, MeasureMatching(valid, invalid));
    }

    private static string Match(ValidationResult validation) => validation switch
    {
        Valid => "valid",
        Invalid(var errors) => errors.Errors["name"][0]
    };

    private static ValidationErrors MatchErrors(ValidationResult validation) => validation switch
    {
        Valid => throw new InvalidOperationException(),
        Invalid(var errors) => errors
    };

    private static string MatchDefault(ValidationResult validation) => validation switch
    {
        null => "uninitialized",
        Valid => "valid",
        Invalid => "invalid"
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MeasureMatching(ValidationResult valid, ValidationResult invalid)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var count = 0;

        for (var index = 0; index < 1_000; index++)
        {
            if (Match(valid) == "valid" && Match(invalid) == "Required.")
                count++;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(1_000, count);
        return allocated;
    }
}
#endif
