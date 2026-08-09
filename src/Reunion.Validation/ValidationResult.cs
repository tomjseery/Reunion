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
    private readonly UnitResult<ValidationErrors> result;

    private ValidationResult(UnitResult<ValidationErrors> result)
    {
        this.result = result;
    }

    /// <summary>Gets whether validation succeeded.</summary>
    public bool IsValid => this.result.IsSuccess;

    /// <summary>Gets whether validation failed.</summary>
    public bool IsInvalid => this.result.IsFailure;

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
        this.result.Match(valid, invalid);

    /// <summary>Invokes the callback for the active case.</summary>
    /// <param name="valid">The valid-case callback.</param>
    /// <param name="invalid">The invalid-case callback.</param>
    public void Match(Action valid, Action<ValidationErrors> invalid) =>
        this.result.Match(valid, invalid);

    /// <summary>Attempts to retrieve the structured validation errors.</summary>
    /// <param name="errors">The errors when this value is invalid.</param>
    public bool TryGetErrors([NotNullWhen(true)] out ValidationErrors? errors) =>
        this.result.TryGetError(out errors);

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
    public bool Equals(ValidationResult other) => this.result.Equals(other.result);

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) =>
        obj is ValidationResult other && this.Equals(other);

    /// <summary>Returns the case-aware hash code for this value.</summary>
    public override int GetHashCode() => this.result.GetHashCode();

    /// <summary>Returns a stable string representation of this value.</summary>
    public override string ToString()
    {
        if (this.result == default)
            return "Uninitialized";

        return this.result.Match(
            static () => "Valid",
            static errors => $"Invalid({errors})");
    }

    /// <summary>Determines whether two validation results are equal.</summary>
    public static bool operator ==(ValidationResult left, ValidationResult right) =>
        left.Equals(right);

    /// <summary>Determines whether two validation results are not equal.</summary>
    public static bool operator !=(ValidationResult left, ValidationResult right) =>
        !left.Equals(right);

    internal bool HasCase => this.result != default;

    internal void EnsureInitialized()
    {
        _ = this.result.IsSuccess;
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
