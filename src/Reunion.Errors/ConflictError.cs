namespace Reunion.Errors;

/// <summary>Describes an operation that conflicts with current application state.</summary>
public sealed record ConflictError : ErrorDefinition
{
    internal ConflictError(string code, string message)
        : base(code, message)
    {
    }

    /// <inheritdoc />
    public override ErrorKind Kind => ErrorKind.Conflict;
}
