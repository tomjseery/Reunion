using System.Diagnostics.CodeAnalysis;

namespace Reunion;

/// <summary>Represents an optional non-null value.</summary>
public readonly partial struct Option<T> : IEquatable<Option<T>>
    where T : notnull
{
    private const byte SomeTag = 1;

    private readonly byte tag;
    private readonly T? value;

    private Option(T value)
    {
        this.tag = SomeTag;
        this.value = value;
    }

    /// <summary>Gets whether the option contains a value.</summary>
    public bool IsSome => this.tag is SomeTag;

    /// <summary>Gets whether the option contains no value.</summary>
    public bool IsNone => !this.IsSome;

    internal static Option<T> CreateSome(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Option<T>(value);
    }

    /// <summary>Converts a value to a populated option.</summary>
    public static implicit operator Option<T>([AllowNull] T value) => value switch
    {
        null => default,
        Some<T> some => CreateSome(some.Value),
        None _ => default,
        _ => CreateSome(value)
    };

    /// <summary>Invokes the callback for the active case.</summary>
    public TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        return this.IsSome ? some(this.value!) : none();
    }

    /// <summary>Invokes the callback for the active case.</summary>
    public void Match(Action<T> some, Action none)
    {
        ArgumentNullException.ThrowIfNull(some);
        ArgumentNullException.ThrowIfNull(none);

        if (this.IsSome)
            some(this.value!);
        else
            none();
    }

    /// <summary>Attempts to retrieve the successful value.</summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = this.value;
        return this.IsSome;
    }

    /// <summary>Transforms a successful value.</summary>
    public Option<TResult> Map<TResult>(Func<T, TResult> map)
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(map);

        return this.IsSome
            ? Option.Some(map(this.value!))
            : Option.None<TResult>();
    }

    /// <summary>Projects a present value for C# query-expression support.</summary>
    public Option<TResult> Select<TResult>(Func<T, TResult> selector)
        where TResult : notnull =>
        this.Map(selector);

    /// <summary>Composes and projects present values for C# query-expression support.</summary>
    public Option<TResult> SelectMany<TIntermediate, TResult>(
        Func<T, Option<TIntermediate>> bind,
        Func<T, TIntermediate, TResult> project)
        where TIntermediate : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);
        ArgumentNullException.ThrowIfNull(project);

        return this.Bind(value =>
            bind(value).Map(intermediate => project(value, intermediate)));
    }

    /// <summary>Composes the result with another result-producing operation.</summary>
    public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> bind)
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(bind);

        return this.IsSome
            ? bind(this.value!)
            : Option.None<TResult>();
    }

    /// <summary>Supplies an option when none is present.</summary>
    public Option<T> OrElse(Func<Option<T>> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        return this.IsSome ? this : fallback();
    }

    /// <summary>Converts absence to a failure.</summary>
    public Result<T, TError> OrFailure<TError>(TError error)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(error);

        return this.IsSome
            ? Result.Success<T, TError>(this.value!)
            : Result.Failure<T, TError>(error);
    }

    /// <summary>Converts absence to a failure.</summary>
    public Result<T, TError> OrFailure<TError>(Func<TError> errorFactory)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorFactory);

        return this.IsSome
            ? Result.Success<T, TError>(this.value!)
            : Result.Failure<T, TError>(errorFactory());
    }

    /// <summary>Returns the contained value or a fallback.</summary>
    public T ValueOr(T fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);
        return this.IsSome ? this.value! : fallback;
    }

    /// <summary>Supplies an option when none is present.</summary>
    public T ValueOrElse(Func<T> fallback)
    {
        ArgumentNullException.ThrowIfNull(fallback);

        if (this.IsSome)
            return this.value!;

        var value = fallback();
        ArgumentNullException.ThrowIfNull(value);
        return value;
    }

    /// <summary>Determines whether this value equals another value.</summary>
    public bool Equals(Option<T> other) =>
        this.tag == other.tag
        && (this.IsNone || EqualityComparer<T>.Default.Equals(this.value!, other.value!));

    /// <summary>Determines whether this value equals another value.</summary>
    public override bool Equals(object? obj) => obj is Option<T> other && this.Equals(other);

    /// <summary>Returns the hash code for this value.</summary>
    public override int GetHashCode() =>
        this.IsSome
            ? HashCode.Combine(this.tag, EqualityComparer<T>.Default.GetHashCode(this.value!))
            : HashCode.Combine(this.tag);

    /// <summary>Returns a string representation of this value.</summary>
    public override string ToString() => this.IsSome ? $"Some({this.value})" : "None";

    /// <summary>Determines whether two values are equal.</summary>
    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    /// <summary>Determines whether two values are not equal.</summary>
    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);
}

/// <summary>Creates option values.</summary>
public static class Option
{
    /// <summary>Creates an option containing a value.</summary>
    public static Option<T> Some<T>(T value)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(value);
        return Option<T>.CreateSome(value);
    }

    /// <summary>Creates an option containing no value.</summary>
    public static Option<T> None<T>()
        where T : notnull =>
        default;

    /// <summary>Provides an operation on this functional value.</summary>
    public static Option<T> FromNullable<T>(T? value)
        where T : class =>
        value is null ? None<T>() : Some(value);

    /// <summary>Provides an operation on this functional value.</summary>
    public static Option<T> FromNullable<T>(T? value)
        where T : struct =>
        value.HasValue ? Some(value.Value) : None<T>();
}

/// <summary>Provides conversion operations for option values.</summary>
public static class OptionExtensions
{
    /// <summary>Converts a successful result to an option.</summary>
    public static Option<T> ToOption<T>(this T? value)
        where T : class =>
        Option.FromNullable(value);

    /// <summary>Converts a successful result to an option.</summary>
    public static Option<T> ToOption<T>(this T? value)
        where T : struct =>
        Option.FromNullable(value);

    /// <summary>Converts a successful result to an option.</summary>
    public static async Task<Option<T>> ToOption<T>(this Task<T?> source)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(source);
        return (await source.ConfigureAwait(false)).ToOption();
    }
}
