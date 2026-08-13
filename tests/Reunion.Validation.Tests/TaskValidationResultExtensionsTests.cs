using Reunion.Errors;

namespace Reunion.Validation.Tests;

public sealed class TaskValidationResultExtensionsTests
{
    [Fact]
    public async Task TaskSourceMatchAndConversions_MapCompletedCases()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var valid = Task.FromResult(ValidationResult.Valid());
        var invalid = Task.FromResult(ValidationResult.Invalid(errors));
        var actions = new List<string>();

        Assert.Equal("valid", await valid.Match(() => "valid", _ => "invalid"));
        Assert.Equal("invalid", await invalid.Match(() => "valid", _ => "invalid"));
        await valid.Match(() => actions.Add("valid"), _ => actions.Add("invalid"));
        await invalid.Match(() => actions.Add("valid"), _ => actions.Add("invalid"));
        Assert.Equal(UnitResult.Success<ValidationErrors>(), await valid.ToResult());
        Assert.Equal(UnitResult.Failure(errors), await invalid.ToResult());
        Assert.Equal(UnitResult.Failure("mapped"), await invalid.ToResult(_ => "mapped"));
        Assert.Equal(Result.Success<int, ValidationErrors>(42), await valid.ToResult(() => 42));
        Assert.Equal(Result.Failure<int, string>("mapped"),
            await invalid.ToResult(() => 42, _ => "mapped"));
        Assert.Equal(["valid", "invalid"], actions);
    }

    [Fact]
    public async Task MatchAsync_DirectAndTaskReceivers_InvokeOnlySelectedBranchOnce()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var validCalls = 0;
        var invalidCalls = 0;
        Func<Task<int>> valid = () => Task.FromResult(++validCalls);
        Func<ValidationErrors, Task<int>> invalid = _ => Task.FromResult(++invalidCalls);

        Assert.Equal(1, await ValidationResult.Valid().MatchAsync(valid, invalid));
        Assert.Equal(1, await ValidationResult.Invalid(errors).MatchAsync(valid, invalid));
        Assert.Equal(2, await Task.FromResult(ValidationResult.Valid()).MatchAsync(valid, invalid));
        Assert.Equal(2, await Task.FromResult(ValidationResult.Invalid(errors)).MatchAsync(valid, invalid));

        await ValidationResult.Valid().MatchAsync(
            () =>
            {
                validCalls++;
                return Task.CompletedTask;
            },
            _ =>
            {
                invalidCalls++;
                return Task.CompletedTask;
            });
        await Task.FromResult(ValidationResult.Invalid(errors)).MatchAsync(
            () =>
            {
                validCalls++;
                return Task.CompletedTask;
            },
            _ =>
            {
                invalidCalls++;
                return Task.CompletedTask;
            });

        Assert.Equal(3, validCalls);
        Assert.Equal(3, invalidCalls);
    }

    [Fact]
    public async Task AsyncOperations_RejectNullSourcesCallbacksAndCallbackTasks()
    {
        Task<ValidationResult> source = null!;
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(
            ValidationResultTests.CreateErrors(("name", "Required.")));

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => TaskValidationResultExtensions.ToResult(source));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => source.Match(() => 1, _ => 0));
        await Assert.ThrowsAsync<ArgumentNullException>(() => source.Map(() => 1));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.Bind(() => ValidationResult.Valid()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => source.MapError(_ => "mapped"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => source.Tap(() => { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => source.TapError(_ => { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => source.Recover(_ => { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.RecoverWith(_ => ValidationResult.Valid()));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.MapAsync(() => Task.FromResult(1)));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.BindAsync(() => Task.FromResult(ValidationResult.Valid())));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.MapErrorAsync(_ => Task.FromResult("mapped")));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.TapAsync(() => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.TapErrorAsync(_ => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.RecoverAsync(_ => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.RecoverWithAsync(_ => Task.FromResult(ValidationResult.Valid())));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => valid.MatchAsync<int>(null!, _ => Task.FromResult(0)));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => valid.MatchAsync(() => Task.FromResult(1), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => valid.MatchAsync<int>(() => null!, _ => Task.FromResult(0)));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => invalid.MatchAsync<int>(() => Task.FromResult(1), _ => null!));
    }

    [Fact]
    public async Task AsyncOperations_PropagateFaultCancellationExceptionsAndDefaults()
    {
        var expected = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(
            () => Task.FromException<ValidationResult>(expected).ToResult()));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => Task.FromCanceled<ValidationResult>(cancellation.Token).ToResult());
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(
            () => ValidationResult.Valid().MatchAsync(
                () => Task.FromException<int>(expected),
                _ => Task.FromResult(0))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => ValidationResult.Invalid(
                    ValidationResultTests.CreateErrors(("name", "Required.")))
                .MatchAsync(
                    () => Task.CompletedTask,
                    _ => Task.FromCanceled(cancellation.Token)));
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Task.FromResult(default(ValidationResult)).ToResult());
    }

    [Fact]
    public async Task TaskSourceComposition_MapsBindsObservesAndRecoversBothCases()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var valid = Task.FromResult(ValidationResult.Valid());
        var invalid = Task.FromResult(ValidationResult.Invalid(errors));
        var calls = 0;

        Assert.Equal(Result.Success<int, string>(42), await valid.Map(() => 42, _ => "mapped"));
        Assert.Equal(Result.Failure<int, string>("mapped"),
            await invalid.Map(() =>
            {
                calls++;
                return 42;
            }, _ => "mapped"));
        Assert.Equal(ValidationResult.Valid(),
            await valid.Bind(() => ValidationResult.Valid()));
        Assert.Equal(Result.Failure<int, string>("mapped"),
            await invalid.Bind(() =>
            {
                calls++;
                return Result.Success<int, string>(42);
            }, _ => "mapped"));
        Assert.Equal(UnitResult.Failure("mapped"), await invalid.MapError(_ => "mapped"));
        Assert.Equal(ValidationResult.Valid(), await valid.Tap(() => calls++));
        Assert.Equal(ValidationResult.Invalid(errors), await invalid.TapError(_ => calls++));
        Assert.Equal(ValidationResult.Valid(), await invalid.Recover(_ => calls++));
        Assert.Equal(ValidationResult.Valid(),
            await invalid.RecoverWith(_ => ValidationResult.Valid()));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task AsyncCallbackComposition_InvokesOnlySelectedCallbacks()
    {
        var errors = ValidationResultTests.CreateErrors(("name", "Required."));
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(errors);
        var calls = 0;

        Assert.Equal(Result.Success<int, string>(42),
            await valid.MapAsync(() => Task.FromResult(42), _ => "mapped"));
        Assert.Equal(Result.Failure<int, string>("mapped"),
            await invalid.MapAsync(() =>
            {
                calls++;
                return Task.FromResult(42);
            }, _ => "mapped"));
        Assert.Equal(ValidationResult.Valid(),
            await valid.BindAsync(() => Task.FromResult(ValidationResult.Valid())));
        Assert.Equal(Result.Success(),
            await valid.BindAsync(() => Task.FromResult(Result.Success()), _ => "mapped"));
        Assert.Equal(Result.Failure<int>("mapped"),
            await invalid.BindAsync(() => Task.FromResult(Result.Success(42)), _ => "mapped"));
        Assert.Equal(Result.Failure<int, string>("mapped"),
            await invalid.BindAsync(() =>
            {
                calls++;
                return Task.FromResult(Result.Success<int, string>(42));
            }, _ => "mapped"));
        Assert.Equal(UnitResult.Failure("mapped"),
            await invalid.MapErrorAsync(_ => Task.FromResult("mapped")));
        Assert.Equal(ValidationResult.Valid(), await valid.TapAsync(() =>
        {
            calls++;
            return Task.CompletedTask;
        }));
        Assert.Equal(ValidationResult.Invalid(errors), await invalid.TapErrorAsync(_ =>
        {
            calls++;
            return Task.CompletedTask;
        }));
        Assert.Equal(ValidationResult.Valid(), await invalid.RecoverAsync(_ =>
        {
            calls++;
            return Task.CompletedTask;
        }));
        Assert.Equal(ValidationResult.Valid(),
            await invalid.RecoverWithAsync(_ => Task.FromResult(ValidationResult.Valid())));
        Assert.Equal(3, calls);
    }

    [Fact]
    public async Task Composition_RejectsNullCallbacksAndReturnedTasks()
    {
        var valid = ValidationResult.Valid();
        var invalid = ValidationResult.Invalid(
            ValidationResultTests.CreateErrors(("name", "Required.")));

        await Assert.ThrowsAsync<ArgumentNullException>(() => valid.MapAsync<int>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            valid.BindAsync((Func<Task<ValidationResult>>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => valid.MapErrorAsync<string>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => valid.TapAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => invalid.TapErrorAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => invalid.RecoverAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => invalid.RecoverWithAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => valid.MapAsync<int>(() => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            valid.BindAsync(() => (Task<ValidationResult>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            invalid.MapErrorAsync<string>(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => invalid.TapErrorAsync(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => invalid.RecoverAsync(_ => null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            invalid.RecoverWithAsync(_ => null!));
    }

    [Fact]
    public async Task TaskComposition_PropagatesFaultedAndCancelledSources()
    {
        var expected = new TestException();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            Task.FromException<ValidationResult>(expected).Map(() => 42)));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            Task.FromCanceled<ValidationResult>(cancellation.Token).Bind(
                () => ValidationResult.Valid()));
        Assert.Same(expected, await Assert.ThrowsAsync<TestException>(() =>
            ValidationResult.Valid().BindAsync(
                () => Task.FromException<ValidationResult>(expected))));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            ValidationResult.Valid().MapAsync(
                () => Task.FromCanceled<int>(cancellation.Token)));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Task.FromResult(default(ValidationResult)).Map(() => 42));
    }

    private sealed class TestException : Exception;
}
