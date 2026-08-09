namespace Reunion.Errors;

/// <summary>Describes an operation the authenticated caller is not permitted to perform.</summary>
public sealed record ForbiddenError : ErrorDefinition
{
    internal ForbiddenError(string code, string message)
        : base(code, message)
    {
    }

    /// <inheritdoc />
    public override ErrorKind Kind => ErrorKind.Forbidden;
}
