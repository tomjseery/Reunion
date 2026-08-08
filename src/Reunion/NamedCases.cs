namespace Reunion;

/// <summary>Represents a successful result without a value.</summary>
public readonly struct Success : IEquatable<Success>
{
    /// <summary>Determines whether this value equals another success marker.</summary>
    public bool Equals(Success other) => true;

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) => obj is Success;

    /// <summary>Returns the hash code for the success marker.</summary>
    public override int GetHashCode() => 0;

    /// <summary>Returns a string representation of the success marker.</summary>
    public override string ToString() => Result.SuccessText;

    /// <summary>Determines whether two success markers are equal.</summary>
    public static bool operator ==(Success left, Success right) => true;

    /// <summary>Determines whether two success markers are not equal.</summary>
    public static bool operator !=(Success left, Success right) => false;
}

/// <summary>Represents a successful result containing a value.</summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public readonly struct Success<TValue> : IEquatable<Success<TValue>>
    where TValue : notnull
{
    /// <summary>Initializes a new success case.</summary>
    /// <param name="value">The success value.</param>
    public Success(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value);
        this.Value = value;
    }

    /// <summary>Gets the success value.</summary>
    public TValue Value { get; }

    /// <summary>Deconstructs the success case into its value.</summary>
    /// <param name="value">The success value.</param>
    public void Deconstruct(out TValue value) => value = this.Value;

    /// <summary>Determines whether this value equals another success case.</summary>
    public bool Equals(Success<TValue> other) =>
        EqualityComparer<TValue>.Default.Equals(this.Value, other.Value);

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) =>
        obj is Success<TValue> other && this.Equals(other);

    /// <summary>Returns the hash code for the success case.</summary>
    public override int GetHashCode() =>
        EqualityComparer<TValue>.Default.GetHashCode(this.Value!);

    /// <summary>Returns a string representation of the success case.</summary>
    public override string ToString() => $"{Result.SuccessText}({this.Value})";

    /// <summary>Determines whether two success cases are equal.</summary>
    public static bool operator ==(Success<TValue> left, Success<TValue> right) =>
        left.Equals(right);

    /// <summary>Determines whether two success cases are not equal.</summary>
    public static bool operator !=(Success<TValue> left, Success<TValue> right) =>
        !left.Equals(right);
}

/// <summary>Represents a failed result containing an error.</summary>
/// <typeparam name="TError">The type of the failure error.</typeparam>
public readonly struct Failure<TError> : IEquatable<Failure<TError>>
    where TError : notnull
{
    /// <summary>Initializes a new failure case.</summary>
    /// <param name="error">The failure error.</param>
    public Failure(TError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        this.Error = error;
    }

    /// <summary>Gets the failure error.</summary>
    public TError Error { get; }

    /// <summary>Deconstructs the failure case into its error.</summary>
    /// <param name="error">The failure error.</param>
    public void Deconstruct(out TError error) => error = this.Error;

    /// <summary>Determines whether this value equals another failure case.</summary>
    public bool Equals(Failure<TError> other) =>
        EqualityComparer<TError>.Default.Equals(this.Error, other.Error);

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) =>
        obj is Failure<TError> other && this.Equals(other);

    /// <summary>Returns the hash code for the failure case.</summary>
    public override int GetHashCode() =>
        EqualityComparer<TError>.Default.GetHashCode(this.Error!);

    /// <summary>Returns a string representation of the failure case.</summary>
    public override string ToString() => $"{Result.FailureText}({this.Error})";

    /// <summary>Determines whether two failure cases are equal.</summary>
    public static bool operator ==(Failure<TError> left, Failure<TError> right) =>
        left.Equals(right);

    /// <summary>Determines whether two failure cases are not equal.</summary>
    public static bool operator !=(Failure<TError> left, Failure<TError> right) =>
        !left.Equals(right);
}

/// <summary>Represents an option containing a value.</summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
public readonly struct Some<T> : IEquatable<Some<T>>
    where T : notnull
{
    /// <summary>Initializes a new present-value case.</summary>
    /// <param name="value">The contained value.</param>
    public Some(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        this.Value = value;
    }

    /// <summary>Gets the contained value.</summary>
    public T Value { get; }

    /// <summary>Deconstructs the some case into its value.</summary>
    /// <param name="value">The contained value.</param>
    public void Deconstruct(out T value) => value = this.Value;

    /// <summary>Determines whether this value equals another some case.</summary>
    public bool Equals(Some<T> other) =>
        EqualityComparer<T>.Default.Equals(this.Value, other.Value);

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) => obj is Some<T> other && this.Equals(other);

    /// <summary>Returns the hash code for the some case.</summary>
    public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(this.Value!);

    /// <summary>Returns a string representation of the some case.</summary>
    public override string ToString() => $"Some({this.Value})";

    /// <summary>Determines whether two some cases are equal.</summary>
    public static bool operator ==(Some<T> left, Some<T> right) => left.Equals(right);

    /// <summary>Determines whether two some cases are not equal.</summary>
    public static bool operator !=(Some<T> left, Some<T> right) => !left.Equals(right);
}

/// <summary>Represents an option without a value.</summary>
public readonly struct None : IEquatable<None>
{
    /// <summary>Determines whether this value equals another none marker.</summary>
    public bool Equals(None other) => true;

    /// <summary>Determines whether this value equals another object.</summary>
    public override bool Equals(object? obj) => obj is None;

    /// <summary>Returns the hash code for the none marker.</summary>
    public override int GetHashCode() => 0;

    /// <summary>Returns a string representation of the none marker.</summary>
    public override string ToString() => nameof(None);

    /// <summary>Determines whether two none markers are equal.</summary>
    public static bool operator ==(None left, None right) => true;

    /// <summary>Determines whether two none markers are not equal.</summary>
    public static bool operator !=(None left, None right) => false;
}
