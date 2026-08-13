using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ValidationResultCompositionTests
{
    [Fact]
    public void Map_ValidCreatesValueAndInvalidMapsErrorWithoutInvokingValueFactory()
    {
        var errors = CreateErrors();
        var calls = 0;

        var valid = ValidationResult.Valid().Map(() => ++calls, _ => "mapped");
        var invalid = ValidationResult.Invalid(errors).Map(() => ++calls, _ => "mapped");

        Assert.Equal(Result.Success<int, string>(1), valid);
        Assert.Equal(Result.Failure<int, string>("mapped"), invalid);
        Assert.Equal(1, calls);
        Assert.Equal(
            Result.Failure<int, ValidationErrors>(errors),
            ValidationResult.Invalid(errors).Map(() => 42));
    }

    [Fact]
    public void Bind_ValidInvokesContinuationAndInvalidShortCircuitsEveryCarrier()
    {
        var errors = CreateErrors();
        var calls = 0;

        Assert.Equal(ValidationResult.Valid(), ValidationResult.Valid().Bind(() =>
        {
            calls++;
            return ValidationResult.Valid();
        }));
        Assert.Equal(ValidationResult.Invalid(errors), ValidationResult.Invalid(errors).Bind(() =>
        {
            calls++;
            return ValidationResult.Valid();
        }));
        Assert.Equal(
            UnitResult.Success<ValidationErrors>(),
            ValidationResult.Valid().Bind(() => UnitResult.Success<ValidationErrors>()));
        Assert.Equal(
            Result.Success<int, ValidationErrors>(42),
            ValidationResult.Valid().Bind(() => Result.Success<int, ValidationErrors>(42)));
        Assert.Equal(Result.Success(),
            ValidationResult.Valid().Bind(Result.Success, _ => "mapped"));
        Assert.Equal(Result.Failure("mapped"),
            ValidationResult.Invalid(errors).Bind(Result.Success, _ => "mapped"));
        Assert.Equal(Result.Success(42),
            ValidationResult.Valid().Bind(() => Result.Success(42), _ => "mapped"));
        Assert.Equal(Result.Failure<int>("mapped"),
            ValidationResult.Invalid(errors).Bind(() => Result.Success(42), _ => "mapped"));
        Assert.Equal(
            UnitResult.Failure("mapped"),
            ValidationResult.Invalid(errors).Bind(() =>
            {
                calls++;
                return UnitResult.Success<string>();
            }, _ => "mapped"));
        Assert.Equal(
            Result.Failure<int, string>("mapped"),
            ValidationResult.Invalid(errors).Bind(() =>
            {
                calls++;
                return Result.Success<int, string>(42);
            }, _ => "mapped"));
        Assert.Equal(1, calls);
    }

    [Fact]
    public void MapErrorTapAndRecovery_PreserveSemanticallyApplicableCarrier()
    {
        var errors = CreateErrors();
        var replacement = ValidationResultTests.CreateErrors(("quantity", "Too large."));
        var validCalls = 0;
        var invalidCalls = 0;

        Assert.Equal(ValidationResult.Valid(), ValidationResult.Valid().MapError(_ => replacement));
        Assert.Equal(ValidationResult.Invalid(replacement),
            ValidationResult.Invalid(errors).MapError(_ => replacement));
        Assert.Equal(UnitResult.Success<string>(), ValidationResult.Valid().MapError(_ => "mapped"));
        Assert.Equal(UnitResult.Failure("mapped"),
            ValidationResult.Invalid(errors).MapError(_ => "mapped"));
        Assert.Equal(ValidationResult.Valid(), ValidationResult.Valid().Tap(() => validCalls++));
        Assert.Equal(ValidationResult.Invalid(errors),
            ValidationResult.Invalid(errors).Tap(() => validCalls++));
        Assert.Equal(ValidationResult.Valid(), ValidationResult.Valid().TapError(_ => invalidCalls++));
        Assert.Equal(ValidationResult.Invalid(errors),
            ValidationResult.Invalid(errors).TapError(_ => invalidCalls++));
        Assert.Equal(ValidationResult.Valid(), ValidationResult.Valid().Recover(_ => invalidCalls++));
        Assert.Equal(ValidationResult.Valid(), ValidationResult.Invalid(errors).Recover(_ => invalidCalls++));
        Assert.Equal(ValidationResult.Valid(),
            ValidationResult.Valid().RecoverWith(_ => ValidationResult.Invalid(replacement)));
        Assert.Equal(ValidationResult.Invalid(replacement),
            ValidationResult.Invalid(errors).RecoverWith(_ => ValidationResult.Invalid(replacement)));
        Assert.Equal(1, validCalls);
        Assert.Equal(2, invalidCalls);
    }

    [Fact]
    public void Composition_RejectsDefaultsNullCallbacksAndUninitializedReturns()
    {
        var uninitialized = default(ValidationResult);
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(CreateErrors());

        Assert.Throws<InvalidOperationException>(() => uninitialized.Map(() => 1));
        Assert.Throws<InvalidOperationException>(() => uninitialized.Bind(() => ValidationResult.Valid()));
        Assert.Throws<ArgumentNullException>(() => valid.Map<int>(null!));
        Assert.Throws<ArgumentNullException>(() => valid.Map<int, string>(() => 1, null!));
        Assert.Throws<ArgumentNullException>(() => valid.Bind((Func<ValidationResult>)null!));
        Assert.Throws<ArgumentNullException>(() => valid.MapError((Func<ValidationErrors, string>)null!));
        Assert.Throws<ArgumentNullException>(() => valid.Tap(null!));
        Assert.Throws<ArgumentNullException>(() => valid.TapError(null!));
        Assert.Throws<ArgumentNullException>(() => valid.Recover(null!));
        Assert.Throws<ArgumentNullException>(() => valid.RecoverWith(null!));
        Assert.Throws<InvalidOperationException>(() => valid.Bind(() => default(ValidationResult)));
        Assert.Throws<InvalidOperationException>(() =>
            valid.Bind(() => default(Result<int, ValidationErrors>)));
        Assert.Throws<ArgumentNullException>(() => invalid.Map(() => 1, _ => (string)null!));
    }

    [Fact]
    public void ImplicitUnitConversion_IsLosslessForAssignmentsArgumentsAndDefaults()
    {
        var errors = CreateErrors();
        UnitResult<ValidationErrors> valid = ValidationResult.Valid();
        UnitResult<ValidationErrors> invalid = ValidationResult.Invalid(errors);
        UnitResult<ValidationErrors> uninitialized = default(ValidationResult);

        Assert.Equal(UnitResult.Success<ValidationErrors>(), valid);
        Assert.Equal(UnitResult.Failure(errors), invalid);
        Assert.Equal(UnitResult.Failure(errors), Accept(ValidationResult.Invalid(errors)));
        Assert.Throws<InvalidOperationException>(() => _ = uninitialized.IsSuccess);
    }

    [Fact]
    public async Task ConcertableStylePipeline_CarriesValidatedValueAndMapsOwnedError()
    {
        var concert = new Concert(7);

        var success = await Result.Success<Concert, CheckoutError>(concert)
            .Bind(value => ValidationResult.Valid().Map<Concert, CheckoutError>(
                () => value,
                errors => new CheckoutError.Invalid(errors)))
            .MapAsync(value => Task.FromResult(value.Id));
        var failure = await Result.Success<Concert, CheckoutError>(concert)
            .Bind(value => ValidationResult.Invalid(CreateErrors()).Map<Concert, CheckoutError>(
                () => value,
                errors => new CheckoutError.Invalid(errors)))
            .MapAsync(value => Task.FromResult(value.Id));

        Assert.Equal(Result.Success<int, CheckoutError>(7), success);
        Assert.True(failure.TryGetError(out var error));
        Assert.IsType<CheckoutError.Invalid>(error);
    }

    private static UnitResult<ValidationErrors> Accept(UnitResult<ValidationErrors> result) => result;

    private static ValidationErrors CreateErrors() =>
        ValidationResultTests.CreateErrors(("name", "Required."));

    private sealed record Concert(int Id);

    private abstract record CheckoutError
    {
        public sealed record Invalid(ValidationErrors Errors) : CheckoutError;
    }
}
