using Reunion;

namespace Reunion.Errors;

/// <summary>Provides semantic Option-to-Result conversions for typed application errors.</summary>
public static class OptionErrorExtensions
{
    /// <summary>Converts absence to a typed not-found failure.</summary>
    public static Result<T, TError> OrNotFound<T, TError>(
        this Option<T> option,
        TError error)
        where T : notnull
        where TError : IError =>
        option.OrFailure(error);

    /// <summary>Lazily converts absence to a typed not-found failure.</summary>
    public static Result<T, TError> OrNotFound<T, TError>(
        this Option<T> option,
        Func<TError> errorFactory)
        where T : notnull
        where TError : IError =>
        option.OrFailure(errorFactory);

    /// <summary>Converts asynchronous absence to a typed not-found failure.</summary>
    public static Task<Result<T, TError>> OrNotFound<T, TError>(
        this Task<Option<T>> source,
        TError error)
        where T : notnull
        where TError : IError =>
        source.OrFailure(error);

    /// <summary>Lazily converts asynchronous absence to a typed not-found failure.</summary>
    public static Task<Result<T, TError>> OrNotFound<T, TError>(
        this Task<Option<T>> source,
        Func<TError> errorFactory)
        where T : notnull
        where TError : IError =>
        source.OrFailure(errorFactory);

    /// <summary>Asynchronously converts absence to a typed not-found failure.</summary>
    public static Task<Result<T, TError>> OrNotFoundAsync<T, TError>(
        this Option<T> option,
        Func<Task<TError>> errorFactory)
        where T : notnull
        where TError : IError =>
        option.OrFailureAsync(errorFactory);

    /// <summary>Asynchronously converts asynchronous absence to a typed not-found failure.</summary>
    public static Task<Result<T, TError>> OrNotFoundAsync<T, TError>(
        this Task<Option<T>> source,
        Func<Task<TError>> errorFactory)
        where T : notnull
        where TError : IError =>
        source.OrFailureAsync(errorFactory);
}
