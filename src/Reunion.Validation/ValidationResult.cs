using System.Diagnostics.CodeAnalysis;
using Reunion.Errors;

namespace Reunion.Validation;

/// <summary>Represents either valid input or immutable structured validation errors.</summary>
/// <remarks>
/// This wrapper adds no allocation or storage overhead relative to
/// <see cref="UnitResult{TError}"/> specialized for <see cref="ValidationErrors"/>.
/// </remarks>
public readonly partial struct ValidationResult : IEquatable<ValidationResult>
{
    private readonly UnitResult<ValidationErrors> unitResult;

    private ValidationResult(UnitResult<ValidationErrors> unitResult)
    {
        this.unitResult = unitResult;
    }

    /// <summary>Gets whether validation succeeded.</summary>
    public bool IsValid => this.unitResult.IsSuccess;

    /// <summary>Gets whether validation failed.</summary>
    public bool IsInvalid => this.unitResult.IsFailure;

    /// <summary>Creates a valid validation result.</summary>
    public static ValidationResult Valid() => new(UnitResult.Success<ValidationErrors>());

    /// <summary>Creates an invalid validation result.</summary>
    /// <param name="errors">The non-empty structured validation errors.</param>
    public static ValidationResult Invalid(ValidationErrors errors) =>
        new(UnitResult.Failure(errors));

    /// <summary>Invokes the callback for the active case.</summary>
    /// <typeparam name="TResult">The callback result type.</typeparam>
    /// <param name="valid">The valid-case callback.</param>
    /// <param name="invalid">The invalid-case callback.</param>
    public TResult Match<TResult>(
        Func<TResult> valid,
        Func<ValidationErrors, TResult> invalid) =>
        this.unitResult.Match(valid, invalid);

    /// <summary>Invokes the callback for the active case.</summary>
    /// <param name="valid">The valid-case callback.</param>
    /// <param name="invalid">The invalid-case callback.</param>
    public void Match(Action valid, Action<ValidationErrors> invalid) =>
        this.unitResult.Match(valid, invalid);

    /// <summary>Attempts to retrieve the structured validation errors.</summary>
    /// <param name="errors">The errors when this value is invalid.</param>
    public bool TryGetErrors([NotNullWhen(true)] out ValidationErrors? errors) =>
        this.unitResult.TryGetError(out errors);

    /// <summary>Creates a value when validation is valid and otherwise preserves the validation errors.</summary>
    public Result<TValue, ValidationErrors> Map<TValue>(Func<TValue> map)
        where TValue : notnull =>
        this.unitResult.Map(map);

    /// <summary>Creates a value when validation is valid and maps validation errors otherwise.</summary>
    public Result<TValue, TError> Map<TValue, TError>(
        Func<TValue> map,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull =>
        this.unitResult.MapError(mapError).Map(map);

    /// <summary>Composes valid validation with another validation.</summary>
    public ValidationResult Bind(Func<ValidationResult> bind)
    {
        ArgumentNullException.ThrowIfNull(bind);
        return new(this.unitResult.Bind(() => bind().unitResult));
    }

    /// <summary>Composes valid validation with a unit result.</summary>
    public UnitResult<ValidationErrors> Bind(Func<UnitResult<ValidationErrors>> bind) =>
        this.unitResult.Bind(bind);

    /// <summary>Composes valid validation with a value-bearing result.</summary>
    public Result<TValue, ValidationErrors> Bind<TValue>(
        Func<Result<TValue, ValidationErrors>> bind)
        where TValue : notnull =>
        this.unitResult.Bind(bind);

    /// <summary>Composes valid validation with a status result and maps validation errors.</summary>
    public Result Bind(Func<Result> bind, Func<ValidationErrors, string> mapError) =>
        this.unitResult.Bind(bind, mapError);

    /// <summary>Composes valid validation with a string-error value result and maps validation errors.</summary>
    public Result<TValue> Bind<TValue>(
        Func<Result<TValue>> bind,
        Func<ValidationErrors, string> mapError)
        where TValue : notnull =>
        this.unitResult.Bind(bind, mapError);

    /// <summary>Composes valid validation with a unit result and maps validation errors.</summary>
    public UnitResult<TError> Bind<TError>(
        Func<UnitResult<TError>> bind,
        Func<ValidationErrors, TError> mapError)
        where TError : notnull =>
        this.unitResult.MapError(mapError).Bind(bind);

    /// <summary>Composes valid validation with a value-bearing result and maps validation errors.</summary>
    public Result<TValue, TError> Bind<TValue, TError>(
        Func<Result<TValue, TError>> bind,
        Func<ValidationErrors, TError> mapError)
        where TValue : notnull
        where TError : notnull =>
        this.unitResult.MapError(mapError).Bind(bind);

    /// <summary>Transforms validation errors while preserving the validation vocabulary.</summary>
    public ValidationResult MapError(Func<ValidationErrors, ValidationErrors> map) =>
        new(this.unitResult.MapError(map));

    /// <summary>Transforms validation errors into another error type.</summary>
    public UnitResult<TError> MapError<TError>(Func<ValidationErrors, TError> map)
        where TError : notnull =>
        this.unitResult.MapError(map);

    /// <summary>Observes valid validation without changing it.</summary>
    public ValidationResult Tap(Action action) => new(this.unitResult.Tap(action));

    /// <summary>Observes invalid validation without changing it.</summary>
    public ValidationResult TapError(Action<ValidationErrors> action) =>
        new(this.unitResult.TapError(action));

    /// <summary>Recovers invalid validation after observing its errors.</summary>
    public ValidationResult Recover(Action<ValidationErrors> fallback) =>
        new(this.unitResult.Recover(fallback));

    /// <summary>Recovers invalid validation with another validation result.</summary>
    public ValidationResult RecoverWith(Func<ValidationErrors, ValidationResult> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return new(this.unitResult.RecoverWith(errors => fallback(errors).unitResult));
    }

    /// <summary>Combines two independent validations, accumulating every error.</summary>
    /// <param name="other">The validation to combine with this value.</param>
    /// <returns>
    /// A valid result when both inputs are valid; otherwise an invalid result. For a repeated field,
    /// left messages precede right messages and duplicates are preserved.
    /// </returns>
    public ValidationResult Combine(ValidationResult other)
    {
        this.EnsureInitialized();
        other.EnsureInitialized();

        if (!this.TryGetErrors(out var left))
            return other;

        if (!other.TryGetErrors(out var right))
            return this;

        return Invalid(Merge(left, right));
    }

    /// <summary>Determines whether this value equals another validation result.</summary>
    public bool Equals(ValidationResult other) => this.unitResult.Equals(other.unitResult);

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) =>
        obj is ValidationResult other && this.Equals(other);

    /// <summary>Returns the case-aware hash code for this value.</summary>
    public override int GetHashCode() => this.unitResult.GetHashCode();

    /// <summary>Returns a stable string representation of this value.</summary>
    public override string ToString()
    {
        if (this.unitResult == default)
            return "Uninitialized";

        return this.unitResult.Match(
            static () => "Valid",
            static errors => $"Invalid({errors})");
    }

    /// <summary>Determines whether two validation results are equal.</summary>
    public static bool operator ==(ValidationResult left, ValidationResult right) =>
        left.Equals(right);

    /// <summary>Determines whether two validation results are not equal.</summary>
    public static bool operator !=(ValidationResult left, ValidationResult right) =>
        !left.Equals(right);

    /// <summary>Converts validation to its lossless unit-result representation.</summary>
    public static implicit operator UnitResult<ValidationErrors>(ValidationResult validation) =>
        validation.unitResult;

    internal UnitResult<ValidationErrors> InnerResult => this.unitResult;

    internal bool HasCase => this.unitResult != default;

    internal static ValidationResult FromUnitResult(UnitResult<ValidationErrors> unitResult) =>
        new(unitResult);

    internal void EnsureInitialized()
    {
        _ = this.unitResult.IsSuccess;
    }

    private static ValidationErrors Merge(ValidationErrors left, ValidationErrors right) =>
        new(Enumerate(left).Concat(Enumerate(right)));

    private static IEnumerable<KeyValuePair<string, string>> Enumerate(ValidationErrors errors)
    {
        foreach (var field in errors.Errors)
        {
            foreach (var message in field.Value)
                yield return new(field.Key, message);
        }
    }
}
