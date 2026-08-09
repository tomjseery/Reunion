using Microsoft.AspNetCore.Mvc;

namespace Reunion.AspNetCore.Mvc;

internal sealed class ApplicationProblemDetailsResult : ObjectResult
{
    internal ApplicationProblemDetailsResult(ProblemDetails problemDetails)
        : base(problemDetails)
    {
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var problemDetails = (ProblemDetails)Value!;
        await ApplicationProblemDetails
            .WriteAsync(context.HttpContext, problemDetails)
            .ConfigureAwait(false);
    }
}
