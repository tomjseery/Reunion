namespace Reunion;

public readonly partial struct Result<TValue>
    where TValue : notnull
{
    /// <summary>Converts a value to a successful result.</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>Converts a named success case to a result.</summary>
    /// <param name="success">The success case.</param>
    public static implicit operator Result<TValue>(Success<TValue> success) =>
        Success(success.Value);

    /// <summary>Converts a named failure case to a result.</summary>
    /// <param name="failure">The failure case.</param>
    public static implicit operator Result<TValue>(Failure<string> failure) =>
        Failure(failure.Error);
}
