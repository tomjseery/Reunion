using Reunion.Errors;

namespace Reunion.Validation;

/// <summary>Represents a valid validation result.</summary>
public readonly struct Valid : IEquatable<Valid>
{
    /// <summary>Determines whether this value equals another valid marker.</summary>
    public bool Equals(Valid other) => true;

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) => obj is Valid;

    /// <summary>Returns the hash code for the valid marker.</summary>
    public override int GetHashCode() => 0;

    /// <summary>Returns a string representation of the valid marker.</summary>
    public override string ToString() => nameof(Valid);

    /// <summary>Determines whether two valid markers are equal.</summary>
    public static bool operator ==(Valid left, Valid right) => true;

    /// <summary>Determines whether two valid markers are not equal.</summary>
    public static bool operator !=(Valid left, Valid right) => false;
}

/// <summary>Represents an invalid validation result containing structured errors.</summary>
public readonly struct Invalid : IEquatable<Invalid>
{
    /// <summary>Initializes a new invalid case.</summary>
    /// <param name="errors">The structured validation errors.</param>
    public Invalid(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        this.Errors = errors;
    }

    /// <summary>Gets the structured validation errors.</summary>
    public ValidationErrors Errors { get; }

    /// <summary>Deconstructs the invalid case into its errors.</summary>
    /// <param name="errors">The structured validation errors.</param>
    public void Deconstruct(out ValidationErrors errors) => errors = this.Errors;

    /// <summary>Determines whether this value equals another invalid case.</summary>
    public bool Equals(Invalid other) => Equals(this.Errors, other.Errors);

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) => obj is Invalid other && this.Equals(other);

    /// <summary>Returns the hash code for the invalid case.</summary>
    public override int GetHashCode() => this.Errors?.GetHashCode() ?? 0;

    /// <summary>Returns a string representation of the invalid case.</summary>
    public override string ToString() => $"{nameof(Invalid)}({this.Errors})";

    /// <summary>Determines whether two invalid cases are equal.</summary>
    public static bool operator ==(Invalid left, Invalid right) => left.Equals(right);

    /// <summary>Determines whether two invalid cases are not equal.</summary>
    public static bool operator !=(Invalid left, Invalid right) => !left.Equals(right);
}
