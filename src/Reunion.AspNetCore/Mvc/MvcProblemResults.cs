using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;

namespace Reunion.AspNetCore.Mvc;

internal static class MvcProblemResults
{
    internal static ObjectResult Map<TError>(
        TError error,
        Func<TError, ProblemDetails> errorMapper)
    {
        var problemDetails = errorMapper(error)
            ?? throw new InvalidOperationException("The error mapper returned null.");

        return FromProblemDetails(problemDetails);
    }

    internal static ObjectResult FromProblemDetails(ProblemDetails problemDetails)
    {
        if (problemDetails.Status is null)
            throw new InvalidOperationException("The mapped problem details must specify a status code.");

        var result = new ApplicationProblemDetailsResult(problemDetails)
        {
            StatusCode = problemDetails.Status
        };
        result.ContentTypes.Add(MediaTypeNames.Application.ProblemJson);
        return result;
    }
}
