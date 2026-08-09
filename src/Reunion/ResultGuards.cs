namespace Reunion;

internal static class ResultGuards
{
    public static void ThrowIfInvalidError<TError>(TError error, string paramName)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(error, paramName);

        if (error is string text)
            ArgumentException.ThrowIfNullOrWhiteSpace(text, paramName);
    }

    public static void ThrowIfStoredErrorIsInvalid<TError>(TError? error, string message)
        where TError : notnull
    {
        if (error is null || error is string text && string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException(message);
    }
}
