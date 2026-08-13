using Reunion.Errors;

namespace Reunion.Validation;

public static partial class TaskValidationResultExtensions
{
    /// <summary>Validates a successful typed-error result after awaiting the source.</summary>
    public static async Task<Result<TValue, TError>> Ensure<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, ValidationResult> validate,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Ensure(validate, mapError);
    }

    /// <summary>Validates a successful string-error result after awaiting the source.</summary>
    public static async Task<Result<TValue>> Ensure<TValue>(
        this Task<Result<TValue>> source,
        Func<TValue, ValidationResult> validate,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Ensure(validate, mapError);
    }

    /// <summary>Asynchronously validates a successful typed-error result while preserving its value.</summary>
    public static Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, Task<ValidationResult>> validate,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        _ = result.IsSuccess;
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.BindAsync(value => EnsureSuccessAsync(value, validate, mapError));
    }

    /// <summary>Asynchronously validates a successful string-error result while preserving its value.</summary>
    public static Task<Result<TValue>> EnsureAsync<TValue>(
        this Result<TValue> result,
        Func<TValue, Task<ValidationResult>> validate,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        _ = result.IsSuccess;
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.BindAsync(value => EnsureSuccessAsync(value, validate, mapError));
    }

    /// <summary>Asynchronously validates a successful typed-error result after awaiting the source.</summary>
    public static async Task<Result<TValue, TError>> EnsureAsync<TValue, TError>(
        this Task<Result<TValue, TError>> source,
        Func<TValue, Task<ValidationResult>> validate,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .EnsureAsync(validate, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously validates a successful string-error result after awaiting the source.</summary>
    public static async Task<Result<TValue>> EnsureAsync<TValue>(
        this Task<Result<TValue>> source,
        Func<TValue, Task<ValidationResult>> validate,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .EnsureAsync(validate, mapError)
            .ConfigureAwait(false);
    }

    private static async Task<Result<TValue, TError>> EnsureSuccessAsync<TValue, TError>(
        TValue value,
        Func<TValue, Task<ValidationResult>> validate,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        var validation = await RequireTask(validate(value)).ConfigureAwait(false);
        return validation.Map<TValue, TError>(() => value, mapError);
    }

    private static async Task<Result<TValue>> EnsureSuccessAsync<TValue>(
        TValue value,
        Func<TValue, Task<ValidationResult>> validate,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        var validation = await RequireTask(validate(value)).ConfigureAwait(false);
        return validation.Bind(() => Result.Success(value), mapError);
    }
}
