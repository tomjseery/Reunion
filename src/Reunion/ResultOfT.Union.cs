#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Reunion;

[Union]
public readonly partial struct Result<TValue> : IUnion, Result<TValue>.IUnionMembers
    where TValue : notnull
{
    /// <summary>Provides the compiler-facing members for the result union.</summary>
    public interface IUnionMembers
    {
        /// <summary>Creates a result from a success case.</summary>
        public static Result<TValue> Create(Success<TValue> success) =>
            Result<TValue>.Success(success.Value);

        /// <summary>Creates a result from a failure case.</summary>
        public static Result<TValue> Create(Failure<string> failure) =>
            Result<TValue>.Failure(failure.Error);

        /// <summary>Gets the active case.</summary>
        public object? Value { get; }

        /// <summary>Gets whether the union contains a case.</summary>
        public bool HasValue { get; }

        /// <summary>Attempts to retrieve the success case.</summary>
        public bool TryGetValue(out Success<TValue> value);

        /// <summary>Attempts to retrieve the failure case.</summary>
        public bool TryGetValue(out Failure<string> value);
    }

    object? IUnion.Value => this.GetUnionValue();

    object? IUnionMembers.Value => this.GetUnionValue();

    private object? GetUnionValue() => this.tag switch
    {
        SuccessTag => new Success<TValue>(this.value!),
        FailureTag => new Failure<string>(this.error!),
        _ => null
    };

    bool IUnionMembers.HasValue => this.tag is SuccessTag or FailureTag;

    bool IUnionMembers.TryGetValue(out Success<TValue> value)
    {
        if (this.tag == SuccessTag)
        {
            value = new Success<TValue>(this.value!);
            return true;
        }

        value = default;
        return false;
    }

    bool IUnionMembers.TryGetValue(out Failure<string> value)
    {
        if (this.tag == FailureTag)
        {
            value = new Failure<string>(this.error!);
            return true;
        }

        value = default;
        return false;
    }
}
#endif
