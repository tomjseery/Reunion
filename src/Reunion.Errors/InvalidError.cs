namespace Reunion.Errors;

/// <summary>Describes an invalid value or request.</summary>
public sealed record InvalidError : ErrorDefinition
{
    internal InvalidError(string code, string message)
        : base(code, message)
    {
    }

    /// <inheritdoc />
    public override ErrorKind Kind => ErrorKind.Invalid;
}
