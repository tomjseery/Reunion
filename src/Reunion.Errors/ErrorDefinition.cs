using System.Text.RegularExpressions;

namespace Reunion.Errors;

/// <summary>Describes a stable, caller-facing application error.</summary>
public abstract partial record ErrorDefinition
{
    private string code;
    private string message;

    internal ErrorDefinition(string code, string message)
    {
        this.code = ValidateCode(code);
        this.message = ValidateMessage(message);
    }

    /// <summary>Gets the stable machine-readable error code.</summary>
    public string Code
    {
        get => this.code;
        init => this.code = ValidateCode(value);
    }

    /// <summary>Gets the safe caller-facing message.</summary>
    public string Message
    {
        get => this.message;
        init => this.message = ValidateMessage(value);
    }

    /// <summary>Gets the transport-neutral semantic classification.</summary>
    public abstract ErrorKind Kind { get; }

    /// <summary>Creates an invalid error definition.</summary>
    public static InvalidError Invalid(string code, string message) =>
        new(code, message);

    /// <summary>Creates a not-found error definition.</summary>
    public static NotFoundError NotFound(string code, string message) =>
        new(code, message);

    /// <summary>Creates a conflict error definition.</summary>
    public static ConflictError Conflict(string code, string message) =>
        new(code, message);

    /// <summary>Creates an unauthenticated error definition.</summary>
    public static UnauthenticatedError Unauthenticated(string code, string message) =>
        new(code, message);

    /// <summary>Creates a forbidden error definition.</summary>
    public static ForbiddenError Forbidden(string code, string message) =>
        new(code, message);

    /// <summary>Creates a payment-required error definition.</summary>
    public static PaymentRequiredError PaymentRequired(string code, string message) =>
        new(code, message);

    /// <summary>Creates a structured validation error definition.</summary>
    public static ValidationError Validation(
        string code,
        string message,
        ValidationErrors errors) =>
        new(code, message, errors);

    /// <summary>Creates factories that derive definitions for cases owned by an error type.</summary>
    public static ErrorDefinitions<TError> For<TError>()
        where TError : IError =>
        default;

    internal static string ValidateCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (!ErrorCodePattern().IsMatch(code))
        {
            throw new ArgumentException(
                "Error codes must contain at least two lowercase dot-separated segments.",
                nameof(code));
        }

        return code;
    }

    private static string ValidateMessage(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        return message;
    }

    [GeneratedRegex(
        @"^[a-z][a-z0-9_]*(?:\.[a-z][a-z0-9_]*)+$",
        RegexOptions.CultureInvariant)]
    private static partial Regex ErrorCodePattern();
}
