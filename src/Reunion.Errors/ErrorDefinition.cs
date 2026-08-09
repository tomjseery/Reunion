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

    /// <summary>Creates an invalid definition with a derived code and message.</summary>
    public static InvalidError Invalid<TCase>() =>
        new(ResolveCode<TCase>(), ResolveMessage<TCase>());

    /// <summary>Creates an invalid definition with a derived code.</summary>
    public static InvalidError Invalid<TCase>(string message) =>
        new(ResolveCode<TCase>(), message);

    /// <summary>Creates a not-found error definition.</summary>
    public static NotFoundError NotFound(string code, string message) =>
        new(code, message);

    /// <summary>Creates a not-found definition with a derived code and message.</summary>
    public static NotFoundError NotFound<TCase>() =>
        new(ResolveCode<TCase>(), ResolveMessage<TCase>());

    /// <summary>Creates a not-found definition with a derived code.</summary>
    public static NotFoundError NotFound<TCase>(string message) =>
        new(ResolveCode<TCase>(), message);

    /// <summary>Creates a conflict error definition.</summary>
    public static ConflictError Conflict(string code, string message) =>
        new(code, message);

    /// <summary>Creates a conflict definition with a derived code and message.</summary>
    public static ConflictError Conflict<TCase>() =>
        new(ResolveCode<TCase>(), ResolveMessage<TCase>());

    /// <summary>Creates a conflict definition with a derived code.</summary>
    public static ConflictError Conflict<TCase>(string message) =>
        new(ResolveCode<TCase>(), message);

    /// <summary>Creates an unauthenticated error definition.</summary>
    public static UnauthenticatedError Unauthenticated(string code, string message) =>
        new(code, message);

    /// <summary>Creates an unauthenticated definition with a derived code and message.</summary>
    public static UnauthenticatedError Unauthenticated<TCase>() =>
        new(ResolveCode<TCase>(), ResolveMessage<TCase>());

    /// <summary>Creates an unauthenticated definition with a derived code.</summary>
    public static UnauthenticatedError Unauthenticated<TCase>(string message) =>
        new(ResolveCode<TCase>(), message);

    /// <summary>Creates a forbidden error definition.</summary>
    public static ForbiddenError Forbidden(string code, string message) =>
        new(code, message);

    /// <summary>Creates a forbidden definition with a derived code and message.</summary>
    public static ForbiddenError Forbidden<TCase>() =>
        new(ResolveCode<TCase>(), ResolveMessage<TCase>());

    /// <summary>Creates a forbidden definition with a derived code.</summary>
    public static ForbiddenError Forbidden<TCase>(string message) =>
        new(ResolveCode<TCase>(), message);

    /// <summary>Creates a payment-required error definition.</summary>
    public static PaymentRequiredError PaymentRequired(string code, string message) =>
        new(code, message);

    /// <summary>Creates a payment-required definition with a derived code and message.</summary>
    public static PaymentRequiredError PaymentRequired<TCase>() =>
        new(ResolveCode<TCase>(), ResolveMessage<TCase>());

    /// <summary>Creates a payment-required definition with a derived code.</summary>
    public static PaymentRequiredError PaymentRequired<TCase>(string message) =>
        new(ResolveCode<TCase>(), message);

    /// <summary>Creates a structured validation error definition.</summary>
    public static ValidationError Validation(
        string code,
        string message,
        ValidationErrors errors) =>
        new(code, message, errors);

    /// <summary>Creates a structured validation definition with a derived code and message.</summary>
    public static ValidationError Validation<TCase>(ValidationErrors errors) =>
        new(ResolveCode<TCase>(), ResolveMessage<TCase>(), errors);

    /// <summary>Creates a structured validation definition with a derived code.</summary>
    public static ValidationError Validation<TCase>(
        string message,
        ValidationErrors errors) =>
        new(ResolveCode<TCase>(), message, errors);

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

    private static string ResolveCode<TCase>()
    {
        var caseType = typeof(TCase);
        var ownerType = caseType.DeclaringType;

        if (ownerType is null || !typeof(IError).IsAssignableFrom(ownerType))
        {
            throw new InvalidOperationException(
                $"Derived error definition factories require {caseType.Name} to be nested directly "
                + "inside its IError owner. Free-standing cases must use an explicit code/message "
                + "factory such as ErrorDefinition.NotFound(\"payment.payer_not_found\", "
                + "\"Payer not found.\").");
        }

        return ErrorCodeResolver.Of(ownerType, caseType);
    }

    private static string ResolveMessage<TCase>() =>
        ErrorMessageResolver.Of(typeof(TCase));

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
