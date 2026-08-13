using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class ResultValidationEnsureExtensionsTests
{
    [Fact]
    public void TypedFailure_IsPreservedWithoutInvokingDelegates()
    {
        var existing = new CheckoutError.Existing();
        var result = Result.Failure<Concert, CheckoutError>(existing);
        var validatorCalls = 0;
        var mapperCalls = 0;

        var ensured = result.Ensure(
            _ =>
            {
                validatorCalls++;
                return ValidationResult.Valid();
            },
            _ =>
            {
                mapperCalls++;
                return new CheckoutError.Invalid(CreateErrors());
            });

        Assert.True(ensured.TryGetError(out var error));
        Assert.Same(existing, error);
        Assert.Equal(0, validatorCalls);
        Assert.Equal(0, mapperCalls);
    }

    [Fact]
    public void TypedSuccess_ValidValidationPreservesExactValueAndInvokesValidatorOnce()
    {
        var concert = new Concert(7);
        var validatorCalls = 0;
        var mapperCalls = 0;

        var ensured = Result.Success<Concert, CheckoutError>(concert).Ensure(
            value =>
            {
                Assert.Same(concert, value);
                validatorCalls++;
                return ValidationResult.Valid();
            },
            _ =>
            {
                mapperCalls++;
                return new CheckoutError.Invalid(CreateErrors());
            });

        Assert.True(ensured.TryGetValue(out var value));
        Assert.Same(concert, value);
        Assert.Equal(1, validatorCalls);
        Assert.Equal(0, mapperCalls);
    }

    [Fact]
    public void TypedSuccess_InvalidValidationMapsExactErrorsOnce()
    {
        var errors = CreateErrors();
        var validatorCalls = 0;
        var mapperCalls = 0;

        var ensured = Result.Success<Concert, CheckoutError>(new Concert(7)).Ensure(
            _ =>
            {
                validatorCalls++;
                return ValidationResult.Invalid(errors);
            },
            received =>
            {
                Assert.Same(errors, received);
                mapperCalls++;
                return new CheckoutError.Invalid(received);
            });

        Assert.True(ensured.TryGetError(out var error));
        var invalid = Assert.IsType<CheckoutError.Invalid>(error);
        Assert.Same(errors, invalid.Errors);
        Assert.Equal(1, validatorCalls);
        Assert.Equal(1, mapperCalls);
    }

    [Fact]
    public void StringResult_HandlesExistingFailureValidAndInvalidValidation()
    {
        var concert = new Concert(7);
        var errors = CreateErrors();
        var calls = 0;

        var existingFailure = Result.Failure<Concert>("existing").Ensure(
            _ =>
            {
                calls++;
                return ValidationResult.Valid();
            },
            _ =>
            {
                calls++;
                return "mapped";
            });
        var valid = Result.Success(concert).Ensure(
            _ =>
            {
                calls++;
                return ValidationResult.Valid();
            },
            _ =>
            {
                calls++;
                return "mapped";
            });
        var invalid = Result.Success(concert).Ensure(
            _ =>
            {
                calls++;
                return ValidationResult.Invalid(errors);
            },
            received =>
            {
                Assert.Same(errors, received);
                calls++;
                return "mapped";
            });

        Assert.Equal(Result.Failure<Concert>("existing"), existingFailure);
        Assert.True(valid.TryGetValue(out var value));
        Assert.Same(concert, value);
        Assert.Equal(Result.Failure<Concert>("mapped"), invalid);
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task TaskReceivers_WithSynchronousValidatorsCoverTypedAndStringResults()
    {
        var concert = new Concert(7);
        var errors = CreateErrors();
        var typedCalls = 0;
        var stringCalls = 0;

        var typed = await Task.FromResult(Result.Success<Concert, CheckoutError>(concert)).Ensure(
            _ =>
            {
                typedCalls++;
                return ValidationResult.Valid();
            },
            received => new CheckoutError.Invalid(received));
        var text = await Task.FromResult(Result.Success(concert)).Ensure(
            _ =>
            {
                stringCalls++;
                return ValidationResult.Invalid(errors);
            },
            _ => "invalid");

        Assert.True(typed.TryGetValue(out var value));
        Assert.Same(concert, value);
        Assert.Equal(Result.Failure<Concert>("invalid"), text);
        Assert.Equal(1, typedCalls);
        Assert.Equal(1, stringCalls);
    }

    [Fact]
    public async Task AsynchronousValidators_CoverBareAndTaskTypedAndStringResults()
    {
        var concert = new Concert(7);
        var errors = CreateErrors();
        var calls = 0;

        var bareTyped = await Result.Success<Concert, CheckoutError>(concert).EnsureAsync(
            _ =>
            {
                calls++;
                return Task.FromResult(ValidationResult.Valid());
            },
            received => new CheckoutError.Invalid(received));
        var taskTyped = await Task.FromResult(Result.Success<Concert, CheckoutError>(concert))
            .EnsureAsync(
                _ =>
                {
                    calls++;
                    return Task.FromResult(ValidationResult.Invalid(errors));
                },
                received => new CheckoutError.Invalid(received));
        var bareString = await Result.Success(concert).EnsureAsync(
            _ =>
            {
                calls++;
                return Task.FromResult(ValidationResult.Valid());
            },
            _ => "invalid");
        var taskString = await Task.FromResult(Result.Success(concert)).EnsureAsync(
            _ =>
            {
                calls++;
                return Task.FromResult(ValidationResult.Invalid(errors));
            },
            _ => "invalid");

        Assert.True(bareTyped.TryGetValue(out var typedValue));
        Assert.Same(concert, typedValue);
        Assert.IsType<CheckoutError.Invalid>(AssertFailure(taskTyped));
        Assert.True(bareString.TryGetValue(out var stringValue));
        Assert.Same(concert, stringValue);
        Assert.Equal(Result.Failure<Concert>("invalid"), taskString);
        Assert.Equal(4, calls);
    }

    [Fact]
    public async Task ExistingFailures_SkipAsynchronousValidatorsAndMappers()
    {
        var existing = new CheckoutError.Existing();
        var calls = 0;

        var result = await Task.FromResult(Result.Failure<Concert, CheckoutError>(existing))
            .EnsureAsync(
                _ =>
                {
                    calls++;
                    return Task.FromResult(ValidationResult.Valid());
                },
                _ =>
                {
                    calls++;
                    return new CheckoutError.Invalid(CreateErrors());
                });

        Assert.Same(existing, AssertFailure(result));
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task NullDelegatesSourcesAndReturnedTasksAreRejected()
    {
        var typed = Result.Success<Concert, CheckoutError>(new Concert(7));
        var text = Result.Success(new Concert(7));
        Func<Concert, ValidationResult> syncValidation = _ => ValidationResult.Valid();
        Func<Concert, Task<ValidationResult>> asyncValidation =
            _ => Task.FromResult(ValidationResult.Valid());
        Func<ValidationErrors, CheckoutError> typedMapper =
            errors => new CheckoutError.Invalid(errors);
        Func<ValidationErrors, string> stringMapper = _ => "invalid";

        Assert.Throws<ArgumentNullException>(() => typed.Ensure(null!, typedMapper));
        Assert.Throws<ArgumentNullException>(() => typed.Ensure(syncValidation, null!));
        Assert.Throws<ArgumentNullException>(() => text.Ensure(null!, stringMapper));
        Assert.Throws<ArgumentNullException>(() => text.Ensure(syncValidation, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => typed.EnsureAsync(null!, typedMapper));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            typed.EnsureAsync(asyncValidation, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => text.EnsureAsync(null!, stringMapper));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            text.EnsureAsync(asyncValidation, null!));

        Task<Result<Concert, CheckoutError>> typedSource = null!;
        Task<Result<Concert>> stringSource = null!;
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            typedSource.Ensure(syncValidation, typedMapper));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            stringSource.Ensure(syncValidation, stringMapper));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            typedSource.EnsureAsync(asyncValidation, typedMapper));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            stringSource.EnsureAsync(asyncValidation, stringMapper));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            typed.EnsureAsync(_ => null!, typedMapper));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            text.EnsureAsync(_ => null!, stringMapper));
    }

    [Fact]
    public async Task UninitializedResultsAndValidationResultsAreRejected()
    {
        Func<ValidationErrors, CheckoutError> mapper =
            errors => new CheckoutError.Invalid(errors);

        Assert.Throws<InvalidOperationException>(() =>
            default(Result<Concert, CheckoutError>).Ensure(
                _ => ValidationResult.Valid(), mapper));
        Assert.Throws<InvalidOperationException>(() =>
            default(Result<Concert>).Ensure(
                _ => ValidationResult.Valid(), _ => "invalid"));
        Assert.Throws<InvalidOperationException>(() =>
            Result.Success<Concert, CheckoutError>(new Concert(7)).Ensure(
                _ => default, mapper));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            default(Result<Concert, CheckoutError>).EnsureAsync(
                _ => Task.FromResult(ValidationResult.Valid()), mapper));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Task.FromResult(default(Result<Concert>)).Ensure(
                _ => ValidationResult.Valid(), _ => "invalid"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Result.Success<Concert, CheckoutError>(new Concert(7)).EnsureAsync(
                _ => Task.FromResult(default(ValidationResult)), mapper));
    }

    [Fact]
    public async Task ExceptionsAndCancellationPropagateWithoutTranslation()
    {
        var expected = new TestException();
        var result = Result.Success<Concert, CheckoutError>(new Concert(7));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(expected, Assert.Throws<TestException>(() =>
            result.Ensure(_ => throw expected, errors => new CheckoutError.Invalid(errors))));
        Assert.Same(expected, Assert.Throws<TestException>(() =>
            result.Ensure(
                _ => ValidationResult.Invalid(CreateErrors()),
                _ => throw expected)));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            result.EnsureAsync(
                _ => Task.FromException<ValidationResult>(expected),
                errors => new CheckoutError.Invalid(errors))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            result.EnsureAsync(
                _ => Task.FromCanceled<ValidationResult>(cancellation.Token),
                errors => new CheckoutError.Invalid(errors)));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Task.FromException<Result<Concert, CheckoutError>>(expected).Ensure(
                _ => ValidationResult.Valid(),
                errors => new CheckoutError.Invalid(errors))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Task.FromCanceled<Result<Concert, CheckoutError>>(cancellation.Token).EnsureAsync(
                _ => Task.FromResult(ValidationResult.Valid()),
                errors => new CheckoutError.Invalid(errors)));
    }

    [Fact]
    public async Task ConcertableShapedPipeline_ValidatesAndPreservesValueBeforeAsyncMapping()
    {
        var concert = new Concert(7);
        var quantity = 2;

        var result = await Result.Success<Concert, CheckoutError>(concert)
            .Ensure(
                value => ValidateTickets(value, quantity),
                errors => new CheckoutError.Invalid(errors))
            .MapAsync(CreateCheckoutAsync);

        Assert.Equal(Result.Success<TicketCheckout, CheckoutError>(new TicketCheckout(7, 2)), result);
    }

    private static CheckoutError AssertFailure(Result<Concert, CheckoutError> result)
    {
        Assert.True(result.TryGetError(out var error));
        return error;
    }

    private static ValidationErrors CreateErrors() =>
        ValidationResultTests.CreateErrors(("quantity", "Too many tickets."));

    private static ValidationResult ValidateTickets(Concert concert, int quantity) =>
        concert.Id > 0 && quantity > 0
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(CreateErrors());

    private static Task<TicketCheckout> CreateCheckoutAsync(Concert concert) =>
        Task.FromResult(new TicketCheckout(concert.Id, 2));

    private sealed record Concert(int Id);

    private sealed record TicketCheckout(int ConcertId, int Quantity);

    private abstract record CheckoutError
    {
        public sealed record Existing : CheckoutError;

        public sealed record Invalid(ValidationErrors Errors) : CheckoutError;
    }

    private sealed class TestException : Exception;
}
