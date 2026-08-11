namespace Reunion;

public readonly partial struct Result<TValue, TError>
    where TValue : notnull
    where TError : notnull
{
    /// <summary>Converts a value to a successful result.</summary>
    public static implicit operator Result<TValue, TError>(TValue value) => Success(value);

    /// <summary>Converts an error to a failed result.</summary>
    public static implicit operator Result<TValue, TError>(TError error) => Failure(error);

    /// <summary>Converts a named success case to a result.</summary>
    /// <param name="success">The success case.</param>
    public static implicit operator Result<TValue, TError>(Success<TValue> success) =>
        Success(success.Value);

    /// <summary>Converts a named failure case to a result.</summary>
    /// <param name="failure">The failure case.</param>
    public static implicit operator Result<TValue, TError>(Failure<TError> failure) =>
        Failure(failure.Error);
}
