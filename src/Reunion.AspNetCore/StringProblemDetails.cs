using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Reunion.AspNetCore;

internal static class StringProblemDetails
{
    internal static ProblemDetails Create(string error) =>
        new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = error
        };
}
