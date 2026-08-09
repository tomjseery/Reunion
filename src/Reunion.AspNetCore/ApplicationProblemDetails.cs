using System.Diagnostics;
using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace Reunion.AspNetCore;

internal static class ApplicationProblemDetails
{
    internal const string CodeExtensionKey = "code";
    internal const string TraceIdExtensionKey = "traceId";

    internal static ProblemDetails Create(HttpStatusCode statusCode, string detail) =>
        Create(statusCode, statusCode.ToReasonPhrase(), detail);

    internal static ProblemDetails Create(
        HttpStatusCode statusCode,
        string title,
        string detail) =>
        new()
        {
            Status = (int)statusCode,
            Title = title,
            Detail = detail
        };

    internal static async Task WriteAsync(
        HttpContext httpContext,
        ProblemDetails problemDetails)
    {
        var statusCode = problemDetails.Status
            ?? throw new InvalidOperationException("ProblemDetails status is required.");
        problemDetails.Instance = httpContext.Request.PathBase.Add(httpContext.Request.Path);
        problemDetails.Extensions[TraceIdExtensionKey] =
            Activity.Current?.Id ?? httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = statusCode;

        var problemDetailsService = httpContext.RequestServices
            .GetService<IProblemDetailsService>();

        if (problemDetailsService is not null)
        {
            var context = new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problemDetails
            };

            if (await problemDetailsService.TryWriteAsync(context).ConfigureAwait(false))
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
