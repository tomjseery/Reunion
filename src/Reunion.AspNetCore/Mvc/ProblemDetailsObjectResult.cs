using System.Diagnostics;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Reunion.AspNetCore.Mvc;

internal sealed class ProblemDetailsObjectResult : ObjectResult
{
    private const string TraceIdExtensionKey = "traceId";
    private readonly int statusCode;

    internal ProblemDetailsObjectResult(ProblemDetails problemDetails)
        : base(problemDetails)
    {
        this.statusCode = problemDetails.Status
            ?? throw new InvalidOperationException(
                "The mapped problem details must specify a status code.");
        StatusCode = this.statusCode;
        ContentTypes.Add(MediaTypeNames.Application.ProblemJson);
    }

    public override async Task ExecuteResultAsync(ActionContext context)
    {
        var httpContext = context.HttpContext;
        var problemDetails = (ProblemDetails)Value!;
        problemDetails.Instance = httpContext.Request.PathBase.Add(httpContext.Request.Path);
        problemDetails.Extensions[TraceIdExtensionKey] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = this.statusCode;

        var problemDetailsService = httpContext.RequestServices
            .GetService<IProblemDetailsService>();

        if (problemDetailsService is not null)
        {
            var problemDetailsContext = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            };

            if (await problemDetailsService
                    .TryWriteAsync(problemDetailsContext)
                    .ConfigureAwait(false))
                return;
        }

        httpContext.Response.ContentType = MediaTypeNames.Application.ProblemJson;
        await JsonSerializer
            .SerializeAsync(
                httpContext.Response.Body,
                problemDetails,
                problemDetails.GetType(),
                JsonSerializerOptions.Web,
                httpContext.RequestAborted)
            .ConfigureAwait(false);
    }
}
