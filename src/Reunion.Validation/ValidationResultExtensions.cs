using Reunion.Errors;

namespace Reunion.Validation;

/// <summary>Provides accumulation and Result-family conversions for validation results.</summary>
public static partial class ValidationResultExtensions
{
    /// <summary>Validates a successful typed-error result while preserving its value.</summary>
    public static Result<TValue, TError> Ensure<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, ValidationResult> validate,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull
    {
        _ = result.IsSuccess;
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.Bind(value =>
            validate(value).Map<TValue, TError>(() => value, mapError));
    }

    /// <summary>Validates a successful string-error result while preserving its value.</summary>
    public static Result<TValue> Ensure<TValue>(
        this Result<TValue> result,
        Func<TValue, ValidationResult> validate,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull
    {
        _ = result.IsSuccess;
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(mapError);

        return result.Bind(value =>
            validate(value).Bind(() => Result.Success(value), mapError));
    }

    /// <summary>Combines independent validations, accumulating every error.</summary>
    /// <param name="source">The validations to combine in enumeration order.</param>
    /// <returns>
    /// A valid result for an empty or entirely valid sequence; otherwise an invalid result. For a
    /// repeated field, earlier messages precede later messages and duplicates are preserved.
    /// </returns>
    public static ValidationResult Combine(this IEnumerable<ValidationResult> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        List<KeyValuePair<string, string>>? accumulated = null;
        foreach (var validation in source)
        {
            if (!validation.TryGetErrors(out var errors))
                continue;

            accumulated ??= [];
            foreach (var field in errors.Errors)
            {
                foreach (var message in field.Value)
                    accumulated.Add(new(field.Key, message));
            }
        }

        return accumulated is null
            ? ValidationResult.Valid()
            : ValidationResult.Invalid(new ValidationErrors(accumulated));
    }

    /// <summary>Converts validation to a unit Result with structured validation errors.</summary>
    /// <param name="validation">The validation to convert.</param>
    public static UnitResult<ValidationErrors> ToResult(this ValidationResult validation)
    {
        validation.EnsureInitialized();
        return validation.TryGetErrors(out var errors)
            ? UnitResult.Failure(errors)
            : UnitResult.Success<ValidationErrors>();
    }

    /// <summary>Converts validation to a unit Result with a mapped error.</summary>
    /// <typeparam name="TError">The mapped error type.</typeparam>
    /// <param name="validation">The validation to convert.</param>
    /// <param name="errorMapper">Maps structured errors when validation is invalid.</param>
    public static UnitResult<TError> ToResult<TError>(
        this ValidationResult validation,
        Func<ValidationErrors, TError> errorMapper)
        where TError : notnull
    {
        validation.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(errorMapper);

        return validation.TryGetErrors(out var errors)
            ? UnitResult.Failure(errorMapper(errors))
            : UnitResult.Success<TError>();
    }

    /// <summary>Converts validation to a value-bearing Result.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="validation">The validation to convert.</param>
    /// <param name="successFactory">Creates the value when validation is valid.</param>
    public static Result<TValue, ValidationErrors> ToResult<TValue>(
        this ValidationResult validation,
        Func<TValue> successFactory)
        where TValue : notnull
    {
        validation.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(successFactory);

        return validation.TryGetErrors(out var errors)
            ? Result.Failure<TValue, ValidationErrors>(errors)
            : Result.Success<TValue, ValidationErrors>(successFactory());
    }

    /// <summary>Converts validation to a value-bearing Result with a mapped error.</summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <typeparam name="TError">The mapped error type.</typeparam>
    /// <param name="validation">The validation to convert.</param>
    /// <param name="successFactory">Creates the value when validation is valid.</param>
    /// <param name="errorMapper">Maps structured errors when validation is invalid.</param>
    public static Result<TValue, TError> ToResult<TValue, TError>(
        this ValidationResult validation,
        Func<TValue> successFactory,
        Func<ValidationErrors, TError> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        validation.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(successFactory);
        ArgumentNullException.ThrowIfNull(errorMapper);

        return validation.TryGetErrors(out var errors)
            ? Result.Failure<TValue, TError>(errorMapper(errors))
            : Result.Success<TValue, TError>(successFactory());
    }

    /// <summary>Attempts to create a named failure for an early return.</summary>
    /// <param name="validation">The validation to inspect.</param>
    /// <param name="failure">The named failure when validation is invalid.</param>
    public static bool TryGetFailure(
        this ValidationResult validation,
        out Failure<ValidationErrors> failure)
    {
        validation.EnsureInitialized();
        if (validation.TryGetErrors(out var errors))
        {
            failure = new Failure<ValidationErrors>(errors);
            return true;
        }

        failure = default;
        return false;
    }

    /// <summary>Attempts to create a mapped named failure for an early return.</summary>
    /// <typeparam name="TError">The mapped error type.</typeparam>
    /// <param name="validation">The validation to inspect.</param>
    /// <param name="errorMapper">Maps structured errors when validation is invalid.</param>
    /// <param name="failure">The named failure when validation is invalid.</param>
    public static bool TryGetFailure<TError>(
        this ValidationResult validation,
        Func<ValidationErrors, TError> errorMapper,
        out Failure<TError> failure)
        where TError : notnull
    {
        validation.EnsureInitialized();
        ArgumentNullException.ThrowIfNull(errorMapper);

        if (validation.TryGetErrors(out var errors))
        {
            failure = new Failure<TError>(errorMapper(errors));
            return true;
        }

        failure = default;
        return false;
    }
}
