#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Reunion;

[Union]
public readonly partial struct UnitResult<TError> :
    IUnion,
    UnitResult<TError>.IUnionMembers
    where TError : notnull
{
    /// <summary>Provides the compiler-facing members for the result union.</summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a result from a success case.</summary>
        public static UnitResult<TError> Create(Success success) => UnitResult<TError>.Success();

        /// <summary>Creates a result from a failure case.</summary>
        public static UnitResult<TError> Create(Failure<TError> failure) =>
            UnitResult<TError>.Failure(failure.Error);

        /// <summary>Gets the active case.</summary>
        public object? Value { get; }

        /// <summary>Gets whether the union contains a case.</summary>
        public bool HasValue { get; }

        /// <summary>Attempts to retrieve the success case.</summary>
        public bool TryGetValue(out Success value);

        /// <summary>Attempts to retrieve the failure case.</summary>
        public bool TryGetValue(out Failure<TError> value);
    }

    object? IUnion.Value => this.GetUnionValue();

    object? IUnionMembers.Value => this.GetUnionValue();

    private object? GetUnionValue() => this.tag switch
    {
        SuccessTag => new Success(),
        FailureTag => new Failure<TError>(this.error!),
        _ => null
    };

    bool IUnionMembers.HasValue => this.tag is SuccessTag or FailureTag;

    bool IUnionMembers.TryGetValue(out Success value)
    {
        value = default;
        return this.tag is SuccessTag;
    }

    bool IUnionMembers.TryGetValue(out Failure<TError> value)
    {
        if (this.tag is FailureTag)
        {
            value = new Failure<TError>(this.error!);
            return true;
        }

        value = default;
        return false;
    }
}
#endif
