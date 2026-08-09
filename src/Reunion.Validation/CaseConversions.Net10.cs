#if !NET11_0_OR_GREATER
namespace Reunion.Validation;

public readonly partial struct ValidationResult
{
    /// <summary>Converts a named valid case to a validation result.</summary>
    /// <param name="valid">The valid case.</param>
    public static implicit operator ValidationResult(Valid valid) => Valid();

    /// <summary>Converts a named invalid case to a validation result.</summary>
    /// <param name="invalid">The invalid case.</param>
    public static implicit operator ValidationResult(Invalid invalid) => Invalid(invalid.Errors);
}
#endif
