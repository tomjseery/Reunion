using Microsoft.AspNetCore.Mvc;

namespace Reunion.AspNetCore.Mvc;

/// <summary>Maps option values to ASP.NET Core MVC action results.</summary>
public static class OptionActionResultExtensions
{
    /// <summary>Maps a present value to OK and absence with a caller-supplied action result.</summary>
    public static ActionResult<T> ToOkOr<T>(
        this Option<T> option,
        Func<ActionResult> alternative)
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(alternative);
        return option.Match<ActionResult<T>>(
            value => new OkObjectResult(value),
            () => MapAlternative(alternative));
    }

    /// <summary>Projects a present value to OK and maps absence with a caller-supplied action result.</summary>
    public static ActionResult<TResult> ToOkOr<T, TResult>(
        this Option<T> option,
        Func<T, TResult> projection,
        Func<ActionResult> alternative)
        where T : notnull
        where TResult : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(alternative);
        return option.Map(projection).ToOkOr(alternative);
    }

    /// <summary>Maps a present value to OK and absence to Not Found.</summary>
    public static ActionResult<T> ToOkOrNotFound<T>(this Option<T> option)
        where T : notnull =>
        option.ToOkOr(() => new NotFoundResult());

    /// <summary>Projects a present value to OK and maps absence to Not Found.</summary>
    public static ActionResult<TResult> ToOkOrNotFound<T, TResult>(
        this Option<T> option,
        Func<T, TResult> projection)
        where T : notnull
        where TResult : notnull =>
        option.ToOkOr(projection, () => new NotFoundResult());

    /// <summary>Maps a present value to OK and absence to No Content.</summary>
    public static ActionResult<T> ToOkOrNoContent<T>(this Option<T> option)
        where T : notnull =>
        option.ToOkOr(() => new NoContentResult());

    /// <summary>Projects a present value to OK and maps absence to No Content.</summary>
    public static ActionResult<TResult> ToOkOrNoContent<T, TResult>(
        this Option<T> option,
        Func<T, TResult> projection)
        where T : notnull
        where TResult : notnull =>
        option.ToOkOr(projection, () => new NoContentResult());

    private static ActionResult MapAlternative(Func<ActionResult> alternative) =>
        alternative()
            ?? throw new InvalidOperationException("The alternative returned null.");
}
