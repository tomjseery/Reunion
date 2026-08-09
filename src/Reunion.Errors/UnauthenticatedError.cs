namespace Reunion.Errors;

/// <summary>Describes an operation that requires an authenticated caller.</summary>
public sealed record UnauthenticatedError : ErrorDefinition
{
    internal UnauthenticatedError(string code, string message)
        : base(code, message)
    {
    }

    /// <inheritdoc />
    public override ErrorKind Kind => ErrorKind.Unauthenticated;
}
