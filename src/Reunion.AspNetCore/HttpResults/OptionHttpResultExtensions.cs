using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Reunion.AspNetCore.HttpResults;

/// <summary>Maps option values to strongly typed ASP.NET Core HTTP results.</summary>
public static class OptionHttpResultExtensions
{
    /// <summary>Maps a present value to OK and absence with a caller-supplied HTTP result.</summary>
    public static Results<Ok<T>, TAlternative> ToOkOr<T, TAlternative>(
        this Option<T> option,
        Func<TAlternative> alternative)
        where T : notnull
        where TAlternative : IResult
    {
        ArgumentNullException.ThrowIfNull(alternative);
        return option.Match<Results<Ok<T>, TAlternative>>(
            value => TypedResults.Ok(value),
            () => MapAlternative(alternative));
    }

    /// <summary>Projects a present value to OK and maps absence with a caller-supplied HTTP result.</summary>
    public static Results<Ok<TResult>, TAlternative> ToOkOr<T, TResult, TAlternative>(
        this Option<T> option,
        Func<T, TResult> projection,
        Func<TAlternative> alternative)
        where T : notnull
        where TResult : notnull
        where TAlternative : IResult
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(alternative);
        return option.Map(projection).ToOkOr(alternative);
    }

    /// <summary>Maps a present value to OK and absence to Not Found.</summary>
    public static Results<Ok<T>, NotFound> ToOkOrNotFound<T>(this Option<T> option)
        where T : notnull =>
        option.ToOkOr(TypedResults.NotFound);

    /// <summary>Projects a present value to OK and maps absence to Not Found.</summary>
    public static Results<Ok<TResult>, NotFound> ToOkOrNotFound<T, TResult>(
        this Option<T> option,
        Func<T, TResult> projection)
        where T : notnull
        where TResult : notnull =>
        option.ToOkOr(projection, TypedResults.NotFound);

    /// <summary>Maps a present value to OK and absence to No Content.</summary>
    public static Results<Ok<T>, NoContent> ToOkOrNoContent<T>(this Option<T> option)
        where T : notnull =>
        option.ToOkOr(TypedResults.NoContent);

    /// <summary>Projects a present value to OK and maps absence to No Content.</summary>
    public static Results<Ok<TResult>, NoContent> ToOkOrNoContent<T, TResult>(
        this Option<T> option,
        Func<T, TResult> projection)
        where T : notnull
        where TResult : notnull =>
        option.ToOkOr(projection, TypedResults.NoContent);

    private static TAlternative MapAlternative<TAlternative>(Func<TAlternative> alternative)
        where TAlternative : IResult =>
        alternative()
            ?? throw new InvalidOperationException("The alternative returned null.");
}
