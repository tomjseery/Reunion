namespace Reunion.Errors;

/// <summary>Describes an operation that cannot proceed until payment succeeds.</summary>
public sealed record PaymentRequiredError : ErrorDefinition
{
    internal PaymentRequiredError(string code, string message)
        : base(code, message)
    {
    }

    /// <inheritdoc />
    public override ErrorKind Kind => ErrorKind.PaymentRequired;
}
