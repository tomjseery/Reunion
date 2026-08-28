namespace Reunion;

/// <summary>Provides nullable-value-type exits for option values.</summary>
public static class OptionValueNullableExtensions
{
    /// <summary>Converts an option to a nullable value type.</summary>
    public static T? ToNullable<T>(this Option<T> option)
        where T : struct =>
        option.TryGetValue(out var value) ? value : null;

    /// <summary>Projects a present value to a nullable value type.</summary>
    public static TResult? ToNullable<T, TResult>(this Option<T> option, Func<T, TResult> map)
        where T : notnull
        where TResult : struct
    {
        ArgumentNullException.ThrowIfNull(map);

        return option.TryGetValue(out var value) ? map(value) : null;
    }
}

/// <summary>Provides nullable-reference exits for option values.</summary>
public static class OptionReferenceNullableExtensions
{
    /// <summary>Converts an option to a nullable reference.</summary>
    public static T? ToNullable<T>(this Option<T> option)
        where T : class =>
        option.TryGetValue(out var value) ? value : null;

    /// <summary>Projects a present value to a nullable reference.</summary>
    public static TResult? ToNullable<T, TResult>(this Option<T> option, Func<T, TResult> map)
        where T : notnull
        where TResult : class
    {
        ArgumentNullException.ThrowIfNull(map);

        return option.TryGetValue(out var value) ? map(value) : null;
    }
}
