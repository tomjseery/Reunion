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

    private sealed class TestException : Exception;
}
