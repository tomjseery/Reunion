using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultConversionTests
{
    [Fact]
    public void ToResult_NoValueConversions_MapBothCasesLazily()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var mappings = 0;

        var directValid = ValidationResult.Valid().ToResult();
        var directInvalid = ValidationResult.Invalid(errors).ToResult();
        var mappedValid = ValidationResult.Valid().ToResult(_ =>
        {
            mappings++;
            return "mapped";
        });
        var mappedInvalid = ValidationResult.Invalid(errors).ToResult(_ =>
        {
            mappings++;
            return "mapped";
        });

        Assert.Equal(UnitResult.Success<ValidationErrors>(), directValid);
        Assert.Equal(UnitResult.Failure(errors), directInvalid);
        Assert.Equal(UnitResult.Success<string>(), mappedValid);
        Assert.Equal(UnitResult.Failure("mapped"), mappedInvalid);
        Assert.Equal(1, mappings);
    }

    [Fact]
    public void ToResult_ValueConversions_InvokeOnlySelectedFactoryOnce()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var successes = 0;
        var mappings = 0;
        Func<int> successFactory = () =>
        {
            successes++;
            return 42;
        };
        Func<ValidationErrors, string> errorMapper = _ =>
        {
            mappings++;
            return "mapped";
        };

        var valid = ValidationResult.Valid().ToResult(successFactory);
        var invalid = ValidationResult.Invalid(errors).ToResult(successFactory);
        var mappedValid = ValidationResult.Valid().ToResult(successFactory, errorMapper);
        var mappedInvalid = ValidationResult.Invalid(errors).ToResult(successFactory, errorMapper);

        Assert.Equal(Result.Success<int, ValidationErrors>(42), valid);
        Assert.Equal(Result.Failure<int, ValidationErrors>(errors), invalid);
        Assert.Equal(Result.Success<int, string>(42), mappedValid);
        Assert.Equal(Result.Failure<int, string>("mapped"), mappedInvalid);
        Assert.Equal(2, successes);
        Assert.Equal(1, mappings);
    }

    [Fact]
    public void ToResult_RejectsNullCallbacksAndProducedPayloads()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(errors);

        Assert.Throws<ArgumentNullException>(() => valid.ToResult<string>((Func<string>)null!));
        Assert.Throws<ArgumentNullException>(() => valid.ToResult<string>((Func<ValidationErrors, string>)null!));
        Assert.Throws<ArgumentNullException>(() => valid.ToResult<string, string>(null!, _ => "error"));
        Assert.Throws<ArgumentNullException>(() => valid.ToResult<string, string>(() => "value", null!));
        Assert.Throws<ArgumentNullException>(() => valid.ToResult(() => (string)null!));
        Assert.Throws<ArgumentNullException>(() => invalid.ToResult<string>(_ => null!));
        Assert.Throws<ArgumentException>(() => invalid.ToResult<string>(_ => " "));
    }

    [Fact]
    public void ToResult_SelectedCallbackException_PropagatesUnchanged()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var expected = new TestException();

        Assert.Same(expected, Assert.Throws<TestException>(
            () => ValidationResult.Valid().ToResult<string>(() => throw expected)));
        Assert.Same(expected, Assert.Throws<TestException>(
            () => ValidationResult.Invalid(errors).ToResult<string>(_ => throw expected)));
    }

    [Fact]
    public void TryGetFailure_BothOverloadsSupportValueAndUnitEarlyReturns()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(errors);
        var mappings = 0;

        Assert.False(valid.TryGetFailure(out Failure<ValidationErrors> missing));
        Assert.Null(missing.Error);
        Assert.False(valid.TryGetFailure(_ =>
        {
            mappings++;
            return "mapped";
        }, out Failure<string> mappedMissing));
        Assert.Null(mappedMissing.Error);
        Assert.True(invalid.TryGetFailure(out var failure));
        Assert.True(invalid.TryGetFailure(_ =>
        {
            mappings++;
            return "mapped";
        }, out var mappedFailure));

        Result<int, ValidationErrors> valueResult = failure;
        UnitResult<ValidationErrors> unitResult = failure;
        Result<int, string> mappedValueResult = mappedFailure;
        UnitResult<string> mappedUnitResult = mappedFailure;

        Assert.Equal(Result.Failure<int, ValidationErrors>(errors), valueResult);
        Assert.Equal(UnitResult.Failure(errors), unitResult);
        Assert.Equal(Result.Failure<int, string>("mapped"), mappedValueResult);
        Assert.Equal(UnitResult.Failure("mapped"), mappedUnitResult);
        Assert.Equal(1, mappings);
        Assert.Throws<ArgumentNullException>(() => valid.TryGetFailure<string>(null!, out _));
        Assert.Throws<ArgumentNullException>(() => invalid.TryGetFailure<string>(_ => null!, out _));
    }

    private sealed class TestException : Exception;
}
