namespace Reunion.Errors;

/// <summary>Describes an invalid request with structured field errors.</summary>
public sealed record ValidationError : ErrorDefinition
{
    private ValidationErrors errors;

    internal ValidationError(string code, string message, ValidationErrors errors)
        : base(code, message)
    {
        this.errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    /// <inheritdoc />
    public override ErrorKind Kind => ErrorKind.Invalid;

    /// <summary>Gets the structured validation errors.</summary>
    public ValidationErrors Errors
    {
        get => this.errors;
        init => this.errors = value ?? throw new ArgumentNullException(nameof(value));
    }
}
