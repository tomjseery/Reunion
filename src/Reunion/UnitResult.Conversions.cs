namespace Reunion;

public readonly partial struct UnitResult<TError>
    where TError : notnull
{
    /// <summary>Converts an error to a failed result.</summary>
    public static implicit operator UnitResult<TError>(TError error) => Failure(error);

    /// <summary>Converts a named success case to a unit result.</summary>
    /// <param name="success">The success case.</param>
    public static implicit operator UnitResult<TError>(Success success) => Success();

    /// <summary>Converts a named failure case to a unit result.</summary>
    /// <param name="failure">The failure case.</param>
    public static implicit operator UnitResult<TError>(Failure<TError> failure) =>
        Failure(failure.Error);
}
