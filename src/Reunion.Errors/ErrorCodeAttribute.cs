namespace Reunion.Errors;

/// <summary>Overrides the error code derived from an error owner and case type.</summary>
[AttributeUsage(
    AttributeTargets.Class | AttributeTargets.Struct,
    AllowMultiple = false,
    Inherited = false)]
public sealed class ErrorCodeAttribute : Attribute
{
    /// <summary>Initializes an error-code override.</summary>
    /// <param name="code">The stable lowercase dot-separated error code.</param>
    public ErrorCodeAttribute(string code)
    {
        this.Code = ErrorDefinition.ValidateCode(code);
    }

    /// <summary>Gets the overridden error code.</summary>
    public string Code { get; }
}
