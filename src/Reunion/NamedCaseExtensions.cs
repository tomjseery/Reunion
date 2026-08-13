namespace Reunion;

/// <summary>Provides inferred conversions to payload-bearing named cases.</summary>
public static class NamedCaseExtensions
{
    /// <summary>Converts a value to a named success case.</summary>
    /// <typeparam name="TValue">The type of the success value.</typeparam>
    /// <param name="value">The success value.</param>
    /// <returns>A named success case containing the value.</returns>
    public static Success<TValue> ToSuccess<TValue>(this TValue value)
        where TValue : notnull => new(value);

    /// <summary>Converts an error to a named failure case.</summary>
    /// <typeparam name="TError">The type of the failure error.</typeparam>
    /// <param name="error">The failure error.</param>
    /// <returns>A named failure case containing the error.</returns>
    public static Failure<TError> ToFailure<TError>(this TError error)
        where TError : notnull => new(error);

    /// <summary>Converts a value to a named present-value case.</summary>
    /// <typeparam name="T">The type of the optional value.</typeparam>
    /// <param name="value">The present value.</param>
    /// <returns>A named present-value case containing the value.</returns>
    public static Some<T> ToSome<T>(this T value)
        where T : notnull => new(value);
}
