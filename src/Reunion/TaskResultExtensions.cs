namespace Reunion;

/// <summary>Provides task-based operations for result values.</summary>
public static partial class TaskResultExtensions
{
    /// <summary>Invokes the callback for the active case.</summary>
    public static async Task<TResult> Match<TValue, TError, TResult>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, TResult> success,
        Func<TError, TResult> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(success, failure);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public static async Task Match<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Action<TValue> success,
        Action<TError> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(success, failure);
    }

    /// <summary>Transforms a successful value.</summary>
    public static async Task<Result<TNext, TError>> Map<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, TNext> map)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Map(map);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public static async Task<Result<TNext, TError>> Bind<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Result<TNext, TError>> bind)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    /// <summary>Composes the result while mapping its existing error to the next error type.</summary>
    public static async Task<Result<TNext, TNextError>> Bind<TValue, TError, TNext, TNextError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Result<TNext, TNextError>> bind,
        Func<TError, TNextError> mapError)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public static async Task<UnitResult<TError>> Bind<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, UnitResult<TError>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    /// <summary>Composes the result while mapping its existing error to the next error type.</summary>
    public static async Task<UnitResult<TNextError>> Bind<TValue, TError, TNextError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, UnitResult<TNextError>> bind,
        Func<TError, TNextError> mapError)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public static async Task<Result> Bind<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Result> bind,
        Func<TError, string> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public static async Task<Result<TNext>> Bind<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Result<TNext>> bind,
        Func<TError, string> mapError)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Transforms the failure error while preserving success.</summary>
    public static async Task<Result<TValue, TNextError>> MapError<TValue, TError, TNextError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, TNextError> map)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    /// <summary>Asynchronously transforms the failure error while preserving success.</summary>
    public static Task<Result<TValue, TNextError>> MapErrorAsync<TValue, TError, TNextError>(
        this Result<TValue, TError> result,
        Func<TError, Task<TNextError>> map)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return result.Match(
            value => Task.FromResult(Result.Success<TValue, TNextError>(value)),
            error => MapFailureAsync<TValue, TError, TNextError>(error, map));
    }

    /// <summary>Asynchronously transforms the failure error while preserving success.</summary>
    public static async Task<Result<TValue, TNextError>> MapErrorAsync<TValue, TError, TNextError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Task<TNextError>> map)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MapErrorAsync(map)
            .ConfigureAwait(false);
    }

    /// <summary>Validates a successful value against a predicate.</summary>
    public static async Task<Result<TValue, TError>> Ensure<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, bool> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Ensure(predicate, errorFactory);
    }

    /// <summary>Observes a success without changing it.</summary>
    public static async Task<Result<TValue, TError>> Tap<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Action<TValue> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    /// <summary>Observes a failure without changing it.</summary>
    public static async Task<Result<TValue, TError>> TapError<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Action<TError> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapError(action);
    }

    /// <summary>Recovers from a failure.</summary>
    public static async Task<Result<TValue, TError>> Recover<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, TValue> fallback)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public static async Task<Result<TValue, TError>> RecoverWith<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Result<TValue, TError>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static Task<TResult> MatchAsync<TValue, TError, TResult>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            value => RequireTask(success(value)),
            error => RequireTask(failure(error)));
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static Task MatchAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task> success,
        Func<TError, Task> failure)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);

        return result.Match(
            value => RequireTask(success(value)),
            error => RequireTask(failure(error)));
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static async Task<TResult> MatchAsync<TValue, TError, TResult>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<TResult>> success,
        Func<TError, Task<TResult>> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static async Task MatchAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task> success,
        Func<TError, Task> failure)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously transforms a successful value.</summary>
    public static Task<Result<TNext, TError>> MapAsync<TValue, TError, TNext>(
        this Result<TValue, TError> result,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);

        return result.Match(
            value => MapSuccessAsync<TValue, TNext, TError>(value, map),
            error => Task.FromResult(Result.Failure<TNext, TError>(error)));
    }

    /// <summary>Asynchronously transforms a successful value.</summary>
    public static async Task<Result<TNext, TError>> MapAsync<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MapAsync(map)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static Task<Result<TNext, TError>> BindAsync<TValue, TError, TNext>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<TNext, TError>>> bind)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(Result.Failure<TNext, TError>(error)));
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static async Task<Result<TNext, TError>> BindAsync<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<Result<TNext, TError>>> bind)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes the result while mapping its existing error.</summary>
    public static Task<Result<TNext, TNextError>> BindAsync<TValue, TError, TNext, TNextError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<TNext, TNextError>>> bind,
        Func<TError, TNextError> mapError)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
        where TNextError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(Result.Failure<TNext, TNextError>(mapError(error))));
    }

    /// <summary>Asynchronously composes the result while mapping its existing error after awaiting the source.</summary>
    public static async Task<Result<TNext, TNextError>> BindAsync<TValue, TError, TNext, TNextError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<Result<TNext, TNextError>>> bind,
        Func<TError, TNextError> mapError)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static Task<UnitResult<TError>> BindAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<UnitResult<TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(UnitResult.Failure(error)));
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static async Task<UnitResult<TError>> BindAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<UnitResult<TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes the result while mapping its existing error.</summary>
    public static Task<UnitResult<TNextError>> BindAsync<TValue, TError, TNextError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<UnitResult<TNextError>>> bind,
        Func<TError, TNextError> mapError)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(UnitResult.Failure(mapError(error))));
    }

    /// <summary>Asynchronously composes the result while mapping its existing error after awaiting the source.</summary>
    public static async Task<UnitResult<TNextError>> BindAsync<TValue, TError, TNextError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<UnitResult<TNextError>>> bind,
        Func<TError, TNextError> mapError)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static Task<Result> BindAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result>> bind,
        Func<TError, string> mapError)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(Result.Failure(mapError(error))));
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static async Task<Result> BindAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<Result>> bind,
        Func<TError, string> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static Task<Result<TNext>> BindAsync<TValue, TError, TNext>(
        this Result<TValue, TError> result,
        Func<TValue, Task<Result<TNext>>> bind,
        Func<TError, string> mapError)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.Match(
            value => BindSuccessAsync(value, bind),
            error => Task.FromResult(Result.Failure<TNext>(mapError(error))));
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static async Task<Result<TNext>> BindAsync<TValue, TError, TNext>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<Result<TNext>>> bind,
        Func<TError, string> mapError)
        where TValue : notnull
        where TError : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously validates a successful value.</summary>
    public static Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<bool>> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);

        return result.Match(
            value => EnsureSuccessAsync(value, predicate, errorFactory),
            error => Task.FromResult(Result.Failure<TValue, TError>(error)));
    }

    /// <summary>Asynchronously validates a successful value.</summary>
    public static async Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<bool>> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .EnsureAsync(predicate, errorFactory)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously observes a success without changing it.</summary>
    public static Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            value => TapSuccessAsync(result, value, action),
            error => Task.FromResult(Result.Failure<TValue, TError>(error)));
    }

    /// <summary>Asynchronously observes a success without changing it.</summary>
    public static async Task<Result<TValue, TError>> TapAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapAsync(action)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously observes a failure without changing it.</summary>
    public static Task<Result<TValue, TError>> TapErrorAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);

        return result.Match(
            value => Task.FromResult(Result.Success<TValue, TError>(value)),
            error => TapFailureAsync(result, error, action));
    }

    /// <summary>Asynchronously observes a failure without changing it.</summary>
    public static async Task<Result<TValue, TError>> TapErrorAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .TapErrorAsync(action)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously recovers from a failure with another result.</summary>
    public static Task<Result<TValue, TError>> RecoverWithAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            value => Task.FromResult(Result.Success<TValue, TError>(value)),
            error => RecoverFailureAsync(error, fallback));
    }

    /// <summary>Asynchronously recovers from a failure with another result.</summary>
    public static async Task<Result<TValue, TError>> RecoverWithAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverWithAsync(fallback)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously recovers from a failure.</summary>
    public static Task<Result<TValue, TError>> RecoverAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, Task<TValue>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);

        return result.Match(
            value => Task.FromResult(Result.Success<TValue, TError>(value)),
            error => RecoverValueAsync<TValue, TError>(error, fallback));
    }

    /// <summary>Asynchronously recovers from a failure.</summary>
    public static async Task<Result<TValue, TError>> RecoverAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TError, Task<TValue>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverAsync(fallback)
            .ConfigureAwait(false);
    }

    private static Task<T> RequireTask<T>(Task<T>? task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return task;
    }

    private static Task RequireTask(Task? task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return task;
    }

    private static async Task<Result<TNext, TError>> MapSuccessAsync<TValue, TNext, TError>(
        TValue value,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TNext : notnull
        where TError : notnull =>
        Result.Success<TNext, TError>(
            await RequireTask(map(value)).ConfigureAwait(false));

    private static async Task<Result<TNext, TError>> BindSuccessAsync<TValue, TNext, TError>(
        TValue value,
        Func<TValue, Task<Result<TNext, TError>>> bind)
        where TValue : notnull
        where TNext : notnull
        where TError : notnull
    {
        var result = await RequireTask(bind(value)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result> BindSuccessAsync<TValue>(
        TValue value,
        Func<TValue, Task<Result>> bind)
        where TValue : notnull
    {
        var result = await RequireTask(bind(value)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result<TNext>> BindSuccessAsync<TValue, TNext>(
        TValue value,
        Func<TValue, Task<Result<TNext>>> bind)
        where TValue : notnull
        where TNext : notnull
    {
        var result = await RequireTask(bind(value)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<UnitResult<TError>> BindSuccessAsync<TValue, TError>(
        TValue value,
        Func<TValue, Task<UnitResult<TError>>> bind)
        where TValue : notnull
        where TError : notnull
    {
        var result = await RequireTask(bind(value)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result<TValue, TError>> EnsureSuccessAsync<TValue, TError>(
        TValue value,
        Func<TValue, Task<bool>> predicate,
        Func<TError> errorFactory)
        where TValue : notnull
        where TError : notnull =>
        await RequireTask(predicate(value)).ConfigureAwait(false)
            ? Result.Success<TValue, TError>(value)
            : Result.Failure<TValue, TError>(errorFactory());

    private static async Task<Result<TValue, TError>> TapSuccessAsync<TValue, TError>(
        Result<TValue, TError> result,
        TValue value,
        Func<TValue, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        await RequireTask(action(value)).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result<TValue, TError>> TapFailureAsync<TValue, TError>(
        Result<TValue, TError> result,
        TError error,
        Func<TError, Task> action)
        where TValue : notnull
        where TError : notnull
    {
        await RequireTask(action(error)).ConfigureAwait(false);
        return result;
    }

    private static async Task<Result<TValue, TError>> RecoverFailureAsync<TValue, TError>(
        TError error,
        Func<TError, Task<Result<TValue, TError>>> fallback)
        where TValue : notnull
        where TError : notnull
    {
        var result = await RequireTask(fallback(error)).ConfigureAwait(false);
        result.EnsureInitialized();
        return result;
    }

    private static async Task<Result<TValue, TNextError>> MapFailureAsync<TValue, TError, TNextError>(
        TError error,
        Func<TError, Task<TNextError>> map)
        where TValue : notnull
        where TError : notnull
        where TNextError : notnull =>
        Result.Failure<TValue, TNextError>(
            await RequireTask(map(error)).ConfigureAwait(false));

    private static async Task<Result<TValue, TError>> RecoverValueAsync<TValue, TError>(
        TError error,
        Func<TError, Task<TValue>> fallback)
        where TValue : notnull
        where TError : notnull =>
        Result.Success<TValue, TError>(
            await RequireTask(fallback(error)).ConfigureAwait(false));
}
