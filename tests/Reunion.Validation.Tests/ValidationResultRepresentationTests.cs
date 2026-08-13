using System.Reflection;
using System.Runtime.CompilerServices;
using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultRepresentationTests
{
    [Fact]
    public void Representation_HasExactlyOneUnitResultFieldAndNoPropertyBackingFields()
    {
        var fields = typeof(ValidationResult).GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var field = Assert.Single(fields);
        Assert.Equal(typeof(UnitResult<ValidationErrors>), field.FieldType);
        Assert.Equal("unitResult", field.Name);
        Assert.DoesNotContain(fields, candidate =>
            candidate.GetCustomAttribute<CompilerGeneratedAttribute>() is not null);
        Assert.Equal(
            Unsafe.SizeOf<UnitResult<ValidationErrors>>(),
            Unsafe.SizeOf<ValidationResult>());
        Assert.True(typeof(Valid).IsValueType);
        Assert.True(typeof(Invalid).IsValueType);
        Assert.True(typeof(ValidationResult).IsValueType);
    }

    [Fact]
    public void Construction_AddsNoAllocationBeyondPreconstructedErrors()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        _ = MeasureConstruction(errors);

        Assert.Equal(0, MeasureConstruction(errors));
    }

    [Fact]
    public void DirectResultConversions_AddNoBoxingAllocation()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(errors);
        _ = MeasureConversions(valid, invalid);

        Assert.Equal(0, MeasureConversions(valid, invalid));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MeasureConstruction(ValidationErrors errors)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var count = 0;

        for (var index = 0; index < 1_000; index++)
        {
            var validCase = new Valid();
            var invalidCase = new Invalid(errors);
            var valid = ValidationResult.Valid();
            var invalid = ValidationResult.Invalid(errors);
            if (validCase == default && invalidCase.Errors == errors && valid.IsValid && invalid.IsInvalid)
                count++;
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(1_000, count);
        return allocated;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long MeasureConversions(ValidationResult valid, ValidationResult invalid)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var count = 0;

        for (var index = 0; index < 1_000; index++)
        {
            var success = valid.ToResult();
            var failure = invalid.ToResult();
            var mappedSuccess = valid.ToResult(static errors => errors);
            var mappedFailure = invalid.ToResult(static errors => errors);
            var valueSuccess = valid.ToResult(static () => 42);
            var valueFailure = invalid.ToResult(static () => 42);
            var fullyMappedSuccess = valid.ToResult(
                static () => 42,
                static errors => errors);
            var fullyMappedFailure = invalid.ToResult(
                static () => 42,
                static errors => errors);
            if (success.IsSuccess
                && failure.IsFailure
                && mappedSuccess.IsSuccess
                && mappedFailure.IsFailure
                && valueSuccess.IsSuccess
                && valueFailure.IsFailure
                && fullyMappedSuccess.IsSuccess
                && fullyMappedFailure.IsFailure)
            {
                count++;
            }
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.Equal(1_000, count);
        return allocated;
    }
}
