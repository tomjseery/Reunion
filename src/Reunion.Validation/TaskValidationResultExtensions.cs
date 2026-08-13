using Reunion.Errors;

namespace Reunion.Validation;

/// <summary>Provides task-based operations for validation results.</summary>
public static class TaskValidationResultExtensions
{
    /// <summary>Creates a value from valid validation after awaiting the source.</summary>
    public static async Task<Result<TValue, ValidationErrors>> Map<TValue>(
        this Task<ValidationResult> source,
        Func<TValue> map)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Map(map);
    }

    /// <summary>Creates a value or maps validation errors after awaiting the source.</summary>
    public static async Task<Result<TValue, TError>> Map<TValue, TError>(
        this Task<ValidationResult> source,
        Func<TValue> map,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Map(map, mapError);
    }

    /// <summary>Composes valid validation with another validation after awaiting the source.</summary>
    public static async Task<ValidationResult> Bind(
        this Task<ValidationResult> source,
        Func<ValidationResult> bind)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    /// <summary>Composes valid validation with a unit result after awaiting the source.</summary>
    public static async Task<UnitResult<ValidationErrors>> Bind(
        this Task<ValidationResult> source,
        Func<UnitResult<ValidationErrors>> bind)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    /// <summary>Composes valid validation with a value-bearing result after awaiting the source.</summary>
    public static async Task<Result<TValue, ValidationErrors>> Bind<TValue>(
        this Task<ValidationResult> source,
        Func<Result<TValue, ValidationErrors>> bind)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind);
    }

    /// <summary>Composes valid validation with a status result and maps validation errors.</summary>
    public static async Task<Result> Bind(
        this Task<ValidationResult> source,
        Func<Result> bind,
        Func<ValidationErrors, string> mapError)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Composes valid validation with a string-error value result and maps validation errors.</summary>
    public static async Task<Result<TValue>> Bind<TValue>(
        this Task<ValidationResult> source,
        Func<Result<TValue>> bind,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Composes valid validation with a unit result and maps validation errors.</summary>
    public static async Task<UnitResult<TError>> Bind<TError>(
        this Task<ValidationResult> source,
        Func<UnitResult<TError>> bind,
        Func<ValidationErrors, TError> mapError)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Composes valid validation with a value-bearing result and maps validation errors.</summary>
    public static async Task<Result<TValue, TError>> Bind<TValue, TError>(
        this Task<ValidationResult> source,
        Func<Result<TValue, TError>> bind,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Bind(bind, mapError);
    }

    /// <summary>Transforms validation errors after awaiting the source.</summary>
    public static async Task<ValidationResult> MapError(
        this Task<ValidationResult> source,
        Func<ValidationErrors, ValidationErrors> map)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    /// <summary>Transforms validation errors into another error type after awaiting the source.</summary>
    public static async Task<UnitResult<TError>> MapError<TError>(
        this Task<ValidationResult> source,
        Func<ValidationErrors, TError> map)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).MapError(map);
    }

    /// <summary>Observes valid validation after awaiting the source.</summary>
    public static async Task<ValidationResult> Tap(
        this Task<ValidationResult> source,
        Action action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Tap(action);
    }

    /// <summary>Observes invalid validation after awaiting the source.</summary>
    public static async Task<ValidationResult> TapError(
        this Task<ValidationResult> source,
        Action<ValidationErrors> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).TapError(action);
    }

    /// <summary>Recovers invalid validation after awaiting the source.</summary>
    public static async Task<ValidationResult> Recover(
        this Task<ValidationResult> source,
        Action<ValidationErrors> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).Recover(fallback);
    }

    /// <summary>Recovers invalid validation with another result after awaiting the source.</summary>
    public static async Task<ValidationResult> RecoverWith(
        this Task<ValidationResult> source,
        Func<ValidationErrors, ValidationResult> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).RecoverWith(fallback);
    }

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

    /// <summary>Asynchronously creates a value when validation is valid.</summary>
    public static Task<Result<TValue, ValidationErrors>> MapAsync<TValue>(
        this ValidationResult validation,
        Func<Task<TValue>> map)
        where TValue : notnull
    {
        return validation.InnerResult.MapAsync<TValue, ValidationErrors>(map);
    }

    /// <summary>Asynchronously creates a value or maps validation errors.</summary>
    public static Task<Result<TValue, TError>> MapAsync<TValue, TError>(
        this ValidationResult validation,
        Func<Task<TValue>> map,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        return validation.InnerResult.MapError(mapError).MapAsync<TValue, TError>(map);
    }

    /// <summary>Asynchronously creates a value after awaiting the validation source.</summary>
    public static async Task<Result<TValue, ValidationErrors>> MapAsync<TValue>(
        this Task<ValidationResult> source,
        Func<Task<TValue>> map)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).MapAsync(map).ConfigureAwait(false);
    }

    /// <summary>Asynchronously creates a value or maps errors after awaiting the validation source.</summary>
    public static async Task<Result<TValue, TError>> MapAsync<TValue, TError>(
        this Task<ValidationResult> source,
        Func<Task<TValue>> map,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .MapAsync(map, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes valid validation with another validation.</summary>
    public static async Task<ValidationResult> BindAsync(
        this ValidationResult validation,
        Func<Task<ValidationResult>> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        var unitResult = await validation.InnerResult
            .BindAsync(() => AwaitUnitResult(bind()))
            .ConfigureAwait(false);
        return ValidationResult.FromUnitResult(unitResult);
    }

    /// <summary>Asynchronously composes valid validation with a unit result.</summary>
    public static Task<UnitResult<ValidationErrors>> BindAsync(
        this ValidationResult validation,
        Func<Task<UnitResult<ValidationErrors>>> bind)
    {
        return validation.InnerResult.BindAsync(bind);
    }

    /// <summary>Asynchronously composes valid validation with a value-bearing result.</summary>
    public static Task<Result<TValue, ValidationErrors>> BindAsync<TValue>(
        this ValidationResult validation,
        Func<Task<Result<TValue, ValidationErrors>>> bind)
        where TValue : notnull
    {
        return validation.InnerResult.BindAsync(bind);
    }

    /// <summary>Asynchronously composes valid validation with a status result.</summary>
    public static Task<Result> BindAsync(
        this ValidationResult validation,
        Func<Task<Result>> bind,
        Func<ValidationErrors, string> mapError)
    {
        return validation.InnerResult.BindAsync(bind, mapError);
    }

    /// <summary>Asynchronously composes valid validation with a string-error value result.</summary>
    public static Task<Result<TValue>> BindAsync<TValue>(
        this ValidationResult validation,
        Func<Task<Result<TValue>>> bind,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        return validation.InnerResult.BindAsync(bind, mapError);
    }

    /// <summary>Asynchronously composes valid validation with a unit result and maps validation errors.</summary>
    public static Task<UnitResult<TError>> BindAsync<TError>(
        this ValidationResult validation,
        Func<Task<UnitResult<TError>>> bind,
        Func<ValidationErrors, TError> mapError)
        where TError : notnull
    {
        return validation.InnerResult.MapError(mapError).BindAsync(bind);
    }

    /// <summary>Asynchronously composes valid validation with a value-bearing result and maps validation errors.</summary>
    public static Task<Result<TValue, TError>> BindAsync<TValue, TError>(
        this ValidationResult validation,
        Func<Task<Result<TValue, TError>>> bind,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        return validation.InnerResult.MapError(mapError).BindAsync(bind);
    }

    /// <summary>Asynchronously composes validation after awaiting the source.</summary>
    public static async Task<ValidationResult> BindAsync(
        this Task<ValidationResult> source,
        Func<Task<ValidationResult>> bind)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).BindAsync(bind).ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes validation with a unit result after awaiting the source.</summary>
    public static async Task<UnitResult<ValidationErrors>> BindAsync(
        this Task<ValidationResult> source,
        Func<Task<UnitResult<ValidationErrors>>> bind)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).BindAsync(bind).ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes validation with a value result after awaiting the source.</summary>
    public static async Task<Result<TValue, ValidationErrors>> BindAsync<TValue>(
        this Task<ValidationResult> source,
        Func<Task<Result<TValue, ValidationErrors>>> bind)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).BindAsync(bind).ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes validation with a status result after awaiting the source.</summary>
    public static async Task<Result> BindAsync(
        this Task<ValidationResult> source,
        Func<Task<Result>> bind,
        Func<ValidationErrors, string> mapError)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes validation with a string-error value result after awaiting the source.</summary>
    public static async Task<Result<TValue>> BindAsync<TValue>(
        this Task<ValidationResult> source,
        Func<Task<Result<TValue>>> bind,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes validation with a mapped unit result after awaiting the source.</summary>
    public static async Task<UnitResult<TError>> BindAsync<TError>(
        this Task<ValidationResult> source,
        Func<Task<UnitResult<TError>>> bind,
        Func<ValidationErrors, TError> mapError)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously composes validation with a mapped value result after awaiting the source.</summary>
    public static async Task<Result<TValue, TError>> BindAsync<TValue, TError>(
        this Task<ValidationResult> source,
        Func<Task<Result<TValue, TError>>> bind,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .BindAsync(bind, mapError)
            .ConfigureAwait(false);
    }

    /// <summary>Asynchronously transforms validation errors.</summary>
    public static async Task<ValidationResult> MapErrorAsync(
        this ValidationResult validation,
        Func<ValidationErrors, Task<ValidationErrors>> map)
    {
        var unitResult = await validation.InnerResult.MapErrorAsync(map).ConfigureAwait(false);
        return ValidationResult.FromUnitResult(unitResult);
    }

    /// <summary>Asynchronously transforms validation errors into another error type.</summary>
    public static Task<UnitResult<TError>> MapErrorAsync<TError>(
        this ValidationResult validation,
        Func<ValidationErrors, Task<TError>> map)
        where TError : notnull
    {
        return validation.InnerResult.MapErrorAsync<ValidationErrors, TError>(map);
    }

    /// <summary>Asynchronously transforms validation errors after awaiting the source.</summary>
    public static async Task<ValidationResult> MapErrorAsync(
        this Task<ValidationResult> source,
        Func<ValidationErrors, Task<ValidationErrors>> map)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).MapErrorAsync(map).ConfigureAwait(false);
    }

    /// <summary>Asynchronously transforms validation errors after awaiting the source.</summary>
    public static async Task<UnitResult<TError>> MapErrorAsync<TError>(
        this Task<ValidationResult> source,
        Func<ValidationErrors, Task<TError>> map)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).MapErrorAsync(map).ConfigureAwait(false);
    }

    /// <summary>Asynchronously observes valid validation.</summary>
    public static async Task<ValidationResult> TapAsync(
        this ValidationResult validation,
        Func<Task> action)
    {
        var unitResult = await validation.InnerResult.TapAsync(action).ConfigureAwait(false);
        return ValidationResult.FromUnitResult(unitResult);
    }

    /// <summary>Asynchronously observes invalid validation.</summary>
    public static async Task<ValidationResult> TapErrorAsync(
        this ValidationResult validation,
        Func<ValidationErrors, Task> action)
    {
        var unitResult = await validation.InnerResult.TapErrorAsync(action).ConfigureAwait(false);
        return ValidationResult.FromUnitResult(unitResult);
    }

    /// <summary>Asynchronously recovers invalid validation after observing its errors.</summary>
    public static async Task<ValidationResult> RecoverAsync(
        this ValidationResult validation,
        Func<ValidationErrors, Task> fallback)
    {
        var unitResult = await validation.InnerResult.RecoverAsync(fallback).ConfigureAwait(false);
        return ValidationResult.FromUnitResult(unitResult);
    }

    /// <summary>Asynchronously recovers invalid validation with another validation.</summary>
    public static async Task<ValidationResult> RecoverWithAsync(
        this ValidationResult validation,
        Func<ValidationErrors, Task<ValidationResult>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        var unitResult = await validation.InnerResult
            .RecoverWithAsync(errors => AwaitUnitResult(fallback(errors)))
            .ConfigureAwait(false);
        return ValidationResult.FromUnitResult(unitResult);
    }

    /// <summary>Asynchronously observes valid validation after awaiting the source.</summary>
    public static async Task<ValidationResult> TapAsync(
        this Task<ValidationResult> source,
        Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).TapAsync(action).ConfigureAwait(false);
    }

    /// <summary>Asynchronously observes invalid validation after awaiting the source.</summary>
    public static async Task<ValidationResult> TapErrorAsync(
        this Task<ValidationResult> source,
        Func<ValidationErrors, Task> action)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).TapErrorAsync(action).ConfigureAwait(false);
    }

    /// <summary>Asynchronously recovers invalid validation after awaiting the source.</summary>
    public static async Task<ValidationResult> RecoverAsync(
        this Task<ValidationResult> source,
        Func<ValidationErrors, Task> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false)).RecoverAsync(fallback).ConfigureAwait(false);
    }

    /// <summary>Asynchronously recovers invalid validation after awaiting the source.</summary>
    public static async Task<ValidationResult> RecoverWithAsync(
        this Task<ValidationResult> source,
        Func<ValidationErrors, Task<ValidationResult>> fallback)
    {
        ArgumentNullException.ThrowIfNull(source);
        return await (await source.ConfigureAwait(false))
            .RecoverWithAsync(fallback)
            .ConfigureAwait(false);
    }

    private static async Task<UnitResult<ValidationErrors>> AwaitUnitResult(
        Task<ValidationResult> validation)
    {
        var result = await RequireTask(validation).ConfigureAwait(false);
        return result.InnerResult;
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
