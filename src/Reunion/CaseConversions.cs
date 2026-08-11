namespace Reunion;

public readonly partial struct Result<TValue>
    where TValue : notnull
{
    /// <summary>Converts a named success case to a result.</summary>
    /// <param name="success">The success case.</param>
    public static implicit operator Result<TValue>(Success<TValue> success) =>
        Success(success.Value);

    /// <summary>Converts a named failure case to a result.</summary>
    /// <param name="failure">The failure case.</param>
    public static implicit operator Result<TValue>(Failure<string> failure) =>
        Failure(failure.Error);
}

public readonly partial struct Result<TValue, TError>
    where TValue : notnull
    where TError : notnull
{
    /// <summary>Converts a named success case to a result.</summary>
    /// <param name="success">The success case.</param>
    public static implicit operator Result<TValue, TError>(Success<TValue> success) =>
        Success(success.Value);

    /// <summary>Converts a named failure case to a result.</summary>
    /// <param name="failure">The failure case.</param>
    public static implicit operator Result<TValue, TError>(Failure<TError> failure) =>
        Failure(failure.Error);
}

public readonly partial struct UnitResult<TError>
    where TError : notnull
{
    /// <summary>Converts a named success case to a unit result.</summary>
    /// <param name="success">The success case.</param>
    public static implicit operator UnitResult<TError>(Success success) => Success();

    /// <summary>Converts a named failure case to a unit result.</summary>
    /// <param name="failure">The failure case.</param>
    public static implicit operator UnitResult<TError>(Failure<TError> failure) =>
        Failure(failure.Error);
}

public readonly partial struct Option<T>
    where T : notnull
{
    /// <summary>Converts a named present-value case to an option.</summary>
    /// <param name="some">The present-value case.</param>
    public static implicit operator Option<T>(Some<T> some) => Option.Some(some.Value);

    /// <summary>Converts a named absent-value case to an option.</summary>
    /// <param name="none">The absent-value case.</param>
    public static implicit operator Option<T>(None none) => Option.None<T>();
}
