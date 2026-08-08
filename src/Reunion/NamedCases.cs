namespace Reunion;

/// <summary>Represents a successful result without a value.</summary>
public readonly struct Success
{
}

/// <summary>Represents a successful result containing a value.</summary>
/// <typeparam name="TValue">The type of the success value.</typeparam>
public readonly struct Success<TValue>
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
}

/// <summary>Represents a failed result containing an error.</summary>
/// <typeparam name="TError">The type of the failure error.</typeparam>
public readonly struct Failure<TError>
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
}

/// <summary>Represents an option containing a value.</summary>
/// <typeparam name="T">The type of the optional value.</typeparam>
public readonly struct Some<T>
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
}

/// <summary>Represents an option without a value.</summary>
public readonly struct None
{
}
