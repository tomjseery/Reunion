namespace Reunion.Errors;

/// <summary>Describes a requested resource that does not exist.</summary>
public sealed record NotFoundError : ErrorDefinition
{
    internal NotFoundError(string code, string message)
        : base(code, message)
    {
    }

    /// <inheritdoc />
    public override ErrorKind Kind => ErrorKind.NotFound;
}
