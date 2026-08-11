using System.Diagnostics.CodeAnalysis;

namespace Reunion;

public readonly partial struct Option<T>
    where T : notnull
{
    /// <summary>Converts a value to a populated option.</summary>
    public static implicit operator Option<T>([AllowNull] T value) =>
        value is null ? default : CreateSome(value);

    /// <summary>Converts a named present-value case to an option.</summary>
    /// <param name="some">The present-value case.</param>
    public static implicit operator Option<T>(Some<T> some) => Option.Some(some.Value);

    /// <summary>Converts a named absent-value case to an option.</summary>
    /// <param name="none">The absent-value case.</param>
    public static implicit operator Option<T>(None none) => Option.None<T>();
}
