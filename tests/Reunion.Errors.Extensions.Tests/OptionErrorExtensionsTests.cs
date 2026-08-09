using Reunion;
using Reunion.Errors;

namespace Reunion.Errors.Extensions.Tests;

public sealed class OptionErrorExtensionsTests
{
    private static readonly LookupError Missing = new LookupError.LookupMissing();

    [Fact]
    public void OrNotFound_EagerError_PreservesSomeAndNone()
    {
        var some = Option.Some(42).OrNotFound(Missing);
        var none = Option.None<int>().OrNotFound(Missing);

        Assert.Equal(Result.Success<int, LookupError>(42), some);
        Assert.Equal(Result.Failure<int, LookupError>(Missing), none);
        Assert.IsType<NotFoundError>(Missing.Definition);
    }

    [Fact]
    public void OrNotFound_LazyFactory_RunsOnceOnlyForNone()
    {
        var calls = 0;
        Func<LookupError> factory = () =>
        {
            calls++;
            return Missing;
        };

        var some = Option.Some(42).OrNotFound(factory);
        Assert.Equal(Result.Success<int, LookupError>(42), some);
        Assert.Equal(0, calls);

        var none = Option.None<int>().OrNotFound(factory);
        Assert.Equal(Result.Failure<int, LookupError>(Missing), none);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task OrNotFound_TaskReceiver_PreservesEagerAndLazySemantics()
    {
        var calls = 0;
        Func<LookupError> factory = () =>
        {
            calls++;
            return Missing;
        };

        var some = await Task.FromResult(Option.Some(42)).OrNotFound(Missing);
        var none = await Task.FromResult(Option.None<int>()).OrNotFound(factory);

        Assert.Equal(Result.Success<int, LookupError>(42), some);
        Assert.Equal(Result.Failure<int, LookupError>(Missing), none);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task OrNotFoundAsync_DirectAndTaskReceivers_RunFactoryOnlyForNone()
    {
        var calls = 0;
        Func<Task<LookupError>> factory = () =>
        {
            calls++;
            return Task.FromResult(Missing);
        };

        var directSome = await Option.Some(42).OrNotFoundAsync(factory);
        var directNone = await Option.None<int>().OrNotFoundAsync(factory);
        var taskSome = await Task.FromResult(Option.Some(42)).OrNotFoundAsync(factory);
        var taskNone = await Task.FromResult(Option.None<int>()).OrNotFoundAsync(factory);

        Assert.Equal(Result.Success<int, LookupError>(42), directSome);
        Assert.Equal(Result.Failure<int, LookupError>(Missing), directNone);
        Assert.Equal(Result.Success<int, LookupError>(42), taskSome);
        Assert.Equal(Result.Failure<int, LookupError>(Missing), taskNone);
        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task OrNotFound_NullArguments_AreRejectedLikeOrFailure()
    {
        LookupError nullError = null!;
        Func<LookupError> nullFactory = null!;
        Func<Task<LookupError>> nullAsyncFactory = null!;
        Task<Option<int>> nullSource = null!;
        var someSource = Task.FromResult(Option.Some(42));

        Assert.Throws<ArgumentNullException>(() => Option.Some(42).OrNotFound(nullError));
        Assert.Throws<ArgumentNullException>(() => Option.Some(42).OrNotFound(nullFactory));
        Assert.Throws<ArgumentNullException>(() =>
        {
            _ = Option.Some(42).OrNotFoundAsync(nullAsyncFactory);
        });
        await Assert.ThrowsAsync<ArgumentNullException>(() => nullSource.OrNotFound(Missing));
        await Assert.ThrowsAsync<ArgumentNullException>(() => nullSource.OrNotFound(() => Missing));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => nullSource.OrNotFoundAsync(() => Task.FromResult(Missing)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => someSource.OrNotFound(nullError));
        await Assert.ThrowsAsync<ArgumentNullException>(() => someSource.OrNotFound(nullFactory));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => someSource.OrNotFoundAsync(nullAsyncFactory));
    }

    [Fact]
    public async Task OrNotFound_NullFactoryResults_AreRejectedLikeOrFailure()
    {
        var none = Option.None<int>();
        var source = Task.FromResult(none);

        Assert.Throws<ArgumentNullException>(() =>
            none.OrNotFound(() => (LookupError)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.OrNotFound(() => (LookupError)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => none.OrNotFoundAsync(() => (Task<LookupError>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => source.OrNotFoundAsync(() => (Task<LookupError>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => none.OrNotFoundAsync(() => Task.FromResult<LookupError>(null!)));
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => source.OrNotFoundAsync(() => Task.FromResult<LookupError>(null!)));
    }

    [Fact]
    public async Task OrNotFound_FactoryAndSourceExceptions_PreserveIdentity()
    {
        var directException = new TestException();
        var taskReceiverException = new TestException();
        var asyncException = new TestException();
        var taskAsyncException = new TestException();
        var sourceException = new TestException();
        Func<LookupError> directFactory = () => throw directException;
        Func<LookupError> taskReceiverFactory = () => throw taskReceiverException;

        Assert.Same(
            directException,
            Assert.Throws<TestException>(
                () => Option.None<int>().OrNotFound(directFactory)));
        Assert.Same(
            taskReceiverException,
            await Assert.ThrowsAsync<TestException>(
                () => Task.FromResult(Option.None<int>())
                    .OrNotFound(taskReceiverFactory)));
        Assert.Same(
            asyncException,
            await Assert.ThrowsAsync<TestException>(
                () => Option.None<int>()
                    .OrNotFoundAsync(() => Task.FromException<LookupError>(asyncException))));
        Assert.Same(
            taskAsyncException,
            await Assert.ThrowsAsync<TestException>(
                () => Task.FromResult(Option.None<int>())
                    .OrNotFoundAsync(() => Task.FromException<LookupError>(taskAsyncException))));
        Assert.Same(
            sourceException,
            await Assert.ThrowsAsync<TestException>(
                () => Task.FromException<Option<int>>(sourceException).OrNotFound(Missing)));
    }

    [Fact]
    public async Task OrNotFound_CanceledSourceAndFactory_RemainCanceled()
    {
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var token = cancellationSource.Token;

        var canceledFactory = Option.None<int>()
            .OrNotFoundAsync(() => Task.FromCanceled<LookupError>(token));
        var canceledSource = Task.FromCanceled<Option<int>>(token).OrNotFound(Missing);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledFactory);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledSource);
        Assert.True(canceledFactory.IsCanceled);
        Assert.True(canceledSource.IsCanceled);
    }

    private abstract record LookupError : IError
    {
        private static readonly ErrorDefinitions<LookupError> Definitions =
            ErrorDefinition.For<LookupError>();

        private LookupError()
        {
        }

        public ErrorDefinition Definition => this switch
        {
            LookupMissing => Definitions.NotFound<LookupMissing>(),
            _ => throw new InvalidOperationException("Unknown error case.")
        };

        public sealed record LookupMissing : LookupError;
    }

    private sealed class TestException : Exception
    {
    }
}
