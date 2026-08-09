#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;
using Reunion.Errors;

namespace Reunion.Validation;

[Union]
public readonly partial struct ValidationResult :
    IUnion,
    ValidationResult.IUnionMembers
{
    /// <summary>Provides the compiler-facing members for the validation union.</summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a validation result from a valid case.</summary>
        public static ValidationResult Create(Valid valid) => ValidationResult.Valid();

        /// <summary>Creates a validation result from an invalid case.</summary>
        public static ValidationResult Create(Invalid invalid) =>
            ValidationResult.Invalid(invalid.Errors);

        /// <summary>Gets the active case.</summary>
        public object? Value { get; }

        /// <summary>Gets whether the union contains a case.</summary>
        public bool HasValue { get; }

        /// <summary>Attempts to retrieve the valid case.</summary>
        public bool TryGetValue(out Valid value);

        /// <summary>Attempts to retrieve the invalid case.</summary>
        public bool TryGetValue(out Invalid value);
    }

    object? IUnion.Value => this.GetUnionValue();

    object? IUnionMembers.Value => this.GetUnionValue();

    private object? GetUnionValue()
    {
        if (!this.HasCase)
            return null;

        if (this.result.TryGetError(out var errors))
            return new Invalid(errors);

        return new Valid();
    }

    bool IUnionMembers.HasValue => this.HasCase;

    bool IUnionMembers.TryGetValue(out Valid value)
    {
        value = default;
        return this.HasCase && this.result.IsSuccess;
    }

    bool IUnionMembers.TryGetValue(out Invalid value)
    {
        if (this.HasCase && this.result.TryGetError(out var errors))
        {
            value = new Invalid(errors);
            return true;
        }

        value = default;
        return false;
    }
}
#endif
