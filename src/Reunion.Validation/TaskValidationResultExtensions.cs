using Reunion.Errors;

namespace Reunion.Validation;

/// <summary>Provides task-based operations for validation results.</summary>
public static class TaskValidationResultExtensions
{
    /// <summary>Invokes the callback for the active case.</summary>
    public static async Task<TResult> Match<TResult>(
        this Task<ValidationResult> source,
        Func<TResult> valid,
        Func<ValidationErrors, TResult> invalid)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Match(valid, invalid);
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public static async Task Match(
        this Task<ValidationResult> source,
        Action valid,
        Action<ValidationErrors> invalid)
    {
        ArgumentNullException.ThrowIfNull(source);
        (await source.ConfigureAwait(false)).Match(valid, invalid);
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static Task<TResult> MatchAsync<TResult>(
        this ValidationResult validation,
        Func<Task<TResult>> valid,
        Func<ValidationErrors, Task<TResult>> invalid)
    {
        validation.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);

        return validation.Match(
            () => RequireTask(valid()),
            errors => RequireTask(invalid(errors)));
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static Task MatchAsync(
        this ValidationResult validation,
        Func<Task> valid,
        Func<ValidationErrors, Task> invalid)
    {
        validation.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(valid);
        ArgumentNullException.ThrowIfNull(invalid);

        return validation.Match(
            () => RequireTask(valid()),
            errors => RequireTask(invalid(errors)));
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static async Task<TResult> MatchAsync<TResult>(
        this Task<ValidationResult> source,
        Func<Task<TResult>> valid,
        Func<ValidationErrors, Task<TResult>> invalid)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MatchAsync(valid, invalid)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously invokes the callback for the active case.</summary>
    public static async Task MatchAsync(
        this Task<ValidationResult> source,
        Func<Task> valid,
        Func<ValidationErrors, Task> invalid)
    {
        ArgumentNullException.ThrowIfNull(source);
        await (await source.ConfigureAwait(false))
            .MatchAsync(valid, invalid)
            .ConfigureAwait(false);
    }

    /// <summary>Converts validation to a unit Result with structured validation errors.</summary>
    public static async Task<UnitResult<ValidationErrors>> ToResult(
        this Task<ValidationResult> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ToResult();
    }

    /// <summary>Converts validation to a unit Result with a mapped error.</summary>
    public static async Task<UnitResult<TError>> ToResult<TError>(
        this Task<ValidationResult> source,
        Func<ValidationErrors, TError> errorMapper)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ToResult(errorMapper);
    }

    /// <summary>Converts validation to a value-bearing Result.</summary>
    public static async Task<Result<TValue, ValidationErrors>> ToResult<TValue>(
        this Task<ValidationResult> source,
        Func<TValue> successFactory)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ToResult(successFactory);
    }

    /// <summary>Converts validation to a value-bearing Result with a mapped error.</summary>
    public static async Task<Result<TValue, TError>> ToResult<TValue, TError>(
        this Task<ValidationResult> source,
        Func<TValue> successFactory,
        Func<ValidationErrors, TError> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ToResult(successFactory, errorMapper);
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
}
