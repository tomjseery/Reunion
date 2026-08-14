using System.Collections.Frozen;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Reunion.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore;

/// <summary>Creates and converts ASP.NET Core problem details.</summary>
public static class ProblemDetailsExtensions
{
    /// <summary>The extension key containing a stable application error code.</summary>
    public const string CodeExtensionKey = "code";

    private static readonly FrozenDictionary<ErrorKind, HttpStatusCode> httpStatusCodes =
        new Dictionary<ErrorKind, HttpStatusCode>
        {
            [ErrorKind.Invalid] = HttpStatusCode.BadRequest,
            [ErrorKind.NotFound] = HttpStatusCode.NotFound,
            [ErrorKind.Conflict] = HttpStatusCode.Conflict,
            [ErrorKind.Unauthenticated] = HttpStatusCode.Unauthorized,
            [ErrorKind.Forbidden] = HttpStatusCode.Forbidden,
            [ErrorKind.PaymentRequired] = HttpStatusCode.PaymentRequired
        }.ToFrozenDictionary();

    extension(ProblemDetails)
    {
        /// <summary>Creates problem details with the standard title for an HTTP status.</summary>
        public static ProblemDetails Create(HttpStatusCode statusCode, string detail) =>
            new()
            {
                Status = (int)statusCode,
                Title = statusCode.ToReasonPhrase(),
                Detail = detail
            };

        /// <summary>Creates validation problem details with the standard title for an HTTP status.</summary>
        public static ValidationProblemDetails Create(
            HttpStatusCode statusCode,
            string detail,
            IDictionary<string, string[]> errors) =>
            new(errors)
            {
                Status = (int)statusCode,
                Title = statusCode.ToReasonPhrase(),
                Detail = detail
            };

        /// <summary>Creates problem details from a typed application error.</summary>
        public static ProblemDetails Create<TError>(TError error)
            where TError : IError
        {
            var definition = error switch
            {
                null => throw new ArgumentNullException(nameof(error)),
                { Definition: null } => throw new InvalidOperationException(
                    "An error definition is required."),
                _ => error.Definition
            };
            var statusCode = httpStatusCodes[definition.Kind];
            var problemDetails = definition is ValidationError validation
                ? ProblemDetails.Create(
                    statusCode,
                    definition.Message,
                    validation.Errors.ToDictionary().ToDictionary(item => item.Key, item => item.Value))
                : ProblemDetails.Create(statusCode, definition.Message);
            problemDetails.Extensions[CodeExtensionKey] = definition.Code;
            return problemDetails;
        }
    }

    extension(ProblemDetails problemDetails)
    {
        internal ObjectResult ToObjectResult() =>
            problemDetails switch
            {
                null => throw new InvalidOperationException("The error mapper returned null."),
                _ => new ProblemDetailsObjectResult(problemDetails)
            };
    }
}
