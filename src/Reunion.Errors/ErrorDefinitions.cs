namespace Reunion.Errors;

/// <summary>Derives stable definitions for cases owned by a typed application error.</summary>
/// <typeparam name="TError">The error union or root type that owns the cases.</typeparam>
public readonly struct ErrorDefinitions<TError>
    where TError : IError
{
    /// <summary>Creates an invalid definition with a derived code and message.</summary>
    public InvalidError Invalid<TCase>() =>
        new(DeriveCode<TCase>(), DeriveMessage<TCase>());

    /// <summary>Creates an invalid definition with a derived code.</summary>
    public InvalidError Invalid<TCase>(string message) =>
        new(DeriveCode<TCase>(), message);

    /// <summary>Creates a not-found definition with a derived code and message.</summary>
    public NotFoundError NotFound<TCase>() =>
        new(DeriveCode<TCase>(), DeriveMessage<TCase>());

    /// <summary>Creates a not-found definition with a derived code.</summary>
    public NotFoundError NotFound<TCase>(string message) =>
        new(DeriveCode<TCase>(), message);

    /// <summary>Creates a conflict definition with a derived code and message.</summary>
    public ConflictError Conflict<TCase>() =>
        new(DeriveCode<TCase>(), DeriveMessage<TCase>());

    /// <summary>Creates a conflict definition with a derived code.</summary>
    public ConflictError Conflict<TCase>(string message) =>
        new(DeriveCode<TCase>(), message);

    /// <summary>Creates an unauthenticated definition with a derived code and message.</summary>
    public UnauthenticatedError Unauthenticated<TCase>() =>
        new(DeriveCode<TCase>(), DeriveMessage<TCase>());

    /// <summary>Creates an unauthenticated definition with a derived code.</summary>
    public UnauthenticatedError Unauthenticated<TCase>(string message) =>
        new(DeriveCode<TCase>(), message);

    /// <summary>Creates a forbidden definition with a derived code and message.</summary>
    public ForbiddenError Forbidden<TCase>() =>
        new(DeriveCode<TCase>(), DeriveMessage<TCase>());

    /// <summary>Creates a forbidden definition with a derived code.</summary>
    public ForbiddenError Forbidden<TCase>(string message) =>
        new(DeriveCode<TCase>(), message);

    /// <summary>Creates a payment-required definition with a derived code and message.</summary>
    public PaymentRequiredError PaymentRequired<TCase>() =>
        new(DeriveCode<TCase>(), DeriveMessage<TCase>());

    /// <summary>Creates a payment-required definition with a derived code.</summary>
    public PaymentRequiredError PaymentRequired<TCase>(string message) =>
        new(DeriveCode<TCase>(), message);

    /// <summary>Creates a validation definition with a derived code and message.</summary>
    public ValidationError Validation<TCase>(ValidationErrors errors) =>
        new(
            DeriveCode<TCase>(),
            DeriveMessage<TCase>(),
            errors);

    /// <summary>Creates a validation definition with a derived code.</summary>
    public ValidationError Validation<TCase>(
        string message,
        ValidationErrors errors) =>
        new(
            DeriveCode<TCase>(),
            message,
            errors);

    private static string DeriveCode<TCase>() =>
        ErrorCodeResolver.Of(typeof(TError), typeof(TCase));

    private static string DeriveMessage<TCase>() =>
        ErrorMessageResolver.Of(typeof(TCase));
}
