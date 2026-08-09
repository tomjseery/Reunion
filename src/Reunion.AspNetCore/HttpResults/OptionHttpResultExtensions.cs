using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Reunion.AspNetCore.HttpResults;

/// <summary>Maps option values to strongly typed ASP.NET Core HTTP results.</summary>
public static class OptionHttpResultExtensions
{
    /// <summary>Maps a present value to OK and absence to Not Found.</summary>
    public static Results<Ok<T>, NotFound> ToOkOrNotFound<T>(this Option<T> option)
        where T : notnull =>
        option.Match<Results<Ok<T>, NotFound>>(
            value => TypedResults.Ok(value),
            () => TypedResults.NotFound());

    /// <summary>Maps a present value to OK and absence to No Content.</summary>
    public static Results<Ok<T>, NoContent> ToOkOrNoContent<T>(this Option<T> option)
        where T : notnull =>
        option.Match<Results<Ok<T>, NoContent>>(
            value => TypedResults.Ok(value),
            () => TypedResults.NoContent());
}
