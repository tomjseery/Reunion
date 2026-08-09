using Microsoft.AspNetCore.Mvc;

namespace Reunion.AspNetCore.Mvc;

/// <summary>Maps option values to ASP.NET Core MVC action results.</summary>
public static class OptionActionResultExtensions
{
    /// <summary>Maps a present value to OK and absence to Not Found.</summary>
    public static ActionResult<T> ToOkOrNotFound<T>(this Option<T> option)
        where T : notnull =>
        option.Match<ActionResult<T>>(
            value => new OkObjectResult(value),
            () => new NotFoundResult());

    /// <summary>Maps a present value to OK and absence to No Content.</summary>
    public static ActionResult<T> ToOkOrNoContent<T>(this Option<T> option)
        where T : notnull =>
        option.Match<ActionResult<T>>(
            value => new OkObjectResult(value),
            () => new NoContentResult());
}
