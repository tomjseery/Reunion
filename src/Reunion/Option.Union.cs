#if NET11_0_OR_GREATER
using System.Runtime.CompilerServices;

namespace Reunion;

[Union]
public readonly partial struct Option<T> : IUnion, Option<T>.IUnionMembers
    where T : notnull
{
    /// <summary>Provides the compiler-facing members for the option union.</summary>
    public interface IUnionMembers
    {
        /// <summary>Creates an option from a present-value case.</summary>
        public static Option<T> Create(Some<T> some) => Option.Some(some.Value);

        /// <summary>Creates an option from an absent-value case.</summary>
        public static Option<T> Create(None none) => Option.None<T>();

        /// <summary>Gets the active case.</summary>
        public object? Value { get; }

        /// <summary>Gets whether the union contains a case.</summary>
        public bool HasValue { get; }

        /// <summary>Attempts to retrieve the present-value case.</summary>
        public bool TryGetValue(out Some<T> value);

        /// <summary>Attempts to retrieve the absent-value case.</summary>
        public bool TryGetValue(out None value);
    }

    object? IUnion.Value => this.GetUnionValue();

    object? IUnionMembers.Value => this.GetUnionValue();

    private object GetUnionValue() => this.IsSome ? new Some<T>(this.value!) : new None();

    bool IUnionMembers.HasValue => true;

    bool IUnionMembers.TryGetValue(out Some<T> value)
    {
        if (this.IsSome)
        {
            value = new Some<T>(this.value!);
            return true;
        }

        value = default;
        return false;
    }

    bool IUnionMembers.TryGetValue(out None value)
    {
        value = default;
        return this.IsNone;
    }
}
#endif
