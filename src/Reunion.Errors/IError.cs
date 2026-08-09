namespace Reunion.Errors;

/// <summary>Exposes the public definition of a typed application error.</summary>
public interface IError
{
    /// <summary>Gets the error's stable public definition.</summary>
    ErrorDefinition Definition { get; }
}
