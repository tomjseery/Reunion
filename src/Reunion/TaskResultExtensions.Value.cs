namespace Reunion;

/// <summary>Provides task-based operations for result values.</summary>
public static partial class TaskResultExtensions
{
    /// <summary>Invokes the callback for the active case.</summary>
    public static async Task<TResult> Match<TValue, TResult>(
        this Task<Result<TValue>> source,
        Func<TValue, TResult> success,
        Func<string, TResult> failure)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(success, failure);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public static async Task Match<TValue>(
        this Task<Result<TValue>> source,
        Action<TValue> success,
        Action<string> failure)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(success, failure);
    }

    /// <summary>Transforms a successful value.</summary>
    public static async Task<Result<TNext>> Map<TValue, TNext>(
        this Task<Result<TValue>> source,
        Func<TValue, TNext> map)
        where TValue : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Map(map);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public static async Task<Result<TNext>> Bind<TValue, TNext>(
        this Task<Result<TValue>> source,
        Func<TValue, Result<TNext>> bind)
        where TValue : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public static async Task<Result> Bind<TValue>(
        this Task<Result<TValue>> source,
        Func<TValue, Result> bind)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    /// <summary>Transforms the failure error while preserving success.</summary>
    public static async Task<Result<TValue, TError>> MapError<TValue, TError>(
        this Task<Result<TValue>> source,
        Func<string, TError> map)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    /// <summary>Validates a successful value against a predicate.</summary>
    public static async Task<Result<TValue>> Ensure<TValue>(
        this Task<Result<TValue>> source,
        Func<TValue, bool> predicate,
        Func<string> errorFactory)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Ensure(predicate, errorFactory);
    }

    /// <summary>Observes a success without changing it.</summary>
    public static async Task<Result<TValue>> Tap<TValue>(
        this Task<Result<TValue>> source,
        Action<TValue> action)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    /// <summary>Observes a failure without changing it.</summary>
    public static async Task<Result<TValue>> TapError<TValue>(
        this Task<Result<TValue>> source,
        Action<string> action)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapError(action);
    }

    /// <summary>Recovers from a failure.</summary>
    public static async Task<Result<TValue>> Recover<TValue>(
        this Task<Result<TValue>> source,
        Func<string, TValue> fallback)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    /// <summary>Recovers from a failure with another result.</summary>
    public static async Task<Result<TValue>> RecoverWith<TValue>(
        this Task<Result<TValue>> source,
        Func<string, Result<TValue>> fallback)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static Task<TResult> MatchAsync<TValue, TResult>(
        this Result<TValue> result,
        Func<TValue, Task<TResult>> success,
        Func<string, Task<TResult>> failure)
        where TValue : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        return result.Match(
            value => RequireTask(success(value)),
            error => RequireTask(failure(error)));
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static Task MatchAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task> success,
        Func<string, Task> failure)
        where TValue : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(success);
        ArgumentNullException.ThrowIfNull(failure);
        return result.Match(
            value => RequireTask(success(value)),
            error => RequireTask(failure(error)));
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static async Task<TResult> MatchAsync<TValue, TResult>(
        this Task<Result<TValue>> source,
        Func<TValue, Task<TResult>> success,
        Func<string, Task<TResult>> failure)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static async Task MatchAsync<TValue>(
        this Task<Result<TValue>> source,
        Func<TValue, Task> success,
        Func<string, Task> failure)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(success, failure)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously transforms a successful value.</summary>
    public static async Task<Result<TNext>> MapAsync<TValue, TNext>(
        this Result<TValue> result,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TNext : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(map);
        if (result.TryGetError(out var error))
            return Result.Failure<TNext>(error);

        result.TryGetValue(out var value);
        return Result.Success(await RequireTask(map(value!)).ConfigureAwait(false));
    }

    /// <summary>Asynchronously transforms a successful value.</summary>
    public static async Task<Result<TNext>> MapAsync<TValue, TNext>(
        this Task<Result<TValue>> source,
        Func<TValue, Task<TNext>> map)
        where TValue : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).MapAsync(map).ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static async Task<Result<TNext>> BindAsync<TValue, TNext>(
        this Result<TValue> result,
        Func<TValue, Task<Result<TNext>>> bind)
        where TValue : notnull
        where TNext : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(bind);
        if (result.TryGetError(out var error))
            return Result.Failure<TNext>(error);

        result.TryGetValue(out var value);
        var bound = await RequireTask(bind(value!)).ConfigureAwait(false);
        bound.EnsureInitialized();
        return bound;
    }

    /// <summary>Asynchronously composes the result with another result-producing operation.</summary>
    public static async Task<Result<TNext>> BindAsync<TValue, TNext>(
        this Task<Result<TValue>> source,
        Func<TValue, Task<Result<TNext>>> bind)
        where TValue : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).BindAsync(bind).ConfigureAwait(false);
    }

    /// <summary>Asynchronously validates a successful value.</summary>
    public static async Task<Result<TValue>> EnsureAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<bool>> predicate,
        Func<string> errorFactory)
        where TValue : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);
        if (result.TryGetError(out _))
            return result;

        result.TryGetValue(out var value);
        return await RequireTask(predicate(value!)).ConfigureAwait(false)
            ? result
            : Result.Failure<TValue>(errorFactory());
    }

    /// <summary>Asynchronously validates a successful value.</summary>
    public static async Task<Result<TValue>> EnsureAsync<TValue>(
        this Task<Result<TValue>> source,
        Func<TValue, Task<bool>> predicate,
        Func<string> errorFactory)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .EnsureAsync(predicate, errorFactory)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously observes a success without changing it.</summary>
    public static async Task<Result<TValue>> TapAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task> action)
        where TValue : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);
        if (result.TryGetValue(out var value))
            await RequireTask(action(value)).ConfigureAwait(false);
        return result;
    }

    /// <summary>Asynchronously observes a success without changing it.</summary>
    public static async Task<Result<TValue>> TapAsync<TValue>(
        this Task<Result<TValue>> source,
        Func<TValue, Task> action)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).TapAsync(action).ConfigureAwait(false);
    }

    /// <summary>Asynchronously observes a failure without changing it.</summary>
    public static async Task<Result<TValue>> TapErrorAsync<TValue>(
        this Result<TValue> result,
        Func<string, Task> action)
        where TValue : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(action);
        if (result.TryGetError(out var error))
            await RequireTask(action(error)).ConfigureAwait(false);
        return result;
    }

    /// <summary>Asynchronously observes a failure without changing it.</summary>
    public static async Task<Result<TValue>> TapErrorAsync<TValue>(
        this Task<Result<TValue>> source,
        Func<string, Task> action)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).TapErrorAsync(action).ConfigureAwait(false);
    }

    /// <summary>Asynchronously recovers from a failure with another result.</summary>
    public static async Task<Result<TValue>> RecoverWithAsync<TValue>(
        this Result<TValue> result,
        Func<string, Task<Result<TValue>>> fallback)
        where TValue : notnull
    {
        result.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(fallback);
        if (!result.TryGetError(out var error))
            return result;

        var recovered = await RequireTask(fallback(error)).ConfigureAwait(false);
        recovered.EnsureInitialized();
        return recovered;
    }

    /// <summary>Asynchronously recovers from a failure with another result.</summary>
    public static async Task<Result<TValue>> RecoverWithAsync<TValue>(
        this Task<Result<TValue>> source,
        Func<string, Task<Result<TValue>>> fallback)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverWithAsync(fallback)
            .ConfigureAwait(false);
    }
}
