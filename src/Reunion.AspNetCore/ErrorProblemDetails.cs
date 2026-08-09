using System.Collections.Frozen;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore;

/// <summary>Creates ASP.NET Core problem details from typed application errors.</summary>
public static class ErrorProblemDetails
{
    /// <summary>The extension key containing the stable application error code.</summary>
    public const string CodeExtensionKey = ApplicationProblemDetails.CodeExtensionKey;

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

    /// <summary>Creates problem details from a typed application error.</summary>
    public static ProblemDetails Create<TError>(TError error)
        where TError : IError
    {
        if (error is null)
            throw new ArgumentNullException(nameof(error));

        var definition = error.Definition
            ?? throw new InvalidOperationException("An error definition is required.");
        var statusCode = httpStatusCodes[definition.Kind];
        var problemDetails = CreateProblemDetails(definition, statusCode);
        problemDetails.Extensions[ApplicationProblemDetails.CodeExtensionKey] = definition.Code;
        return problemDetails;
    }

    private static ProblemDetails CreateProblemDetails(
        ErrorDefinition definition,
        HttpStatusCode statusCode) =>
        definition is ValidationError validation
            ? new ValidationProblemDetails(
                validation.Errors.ToDictionary().ToDictionary(error => error.Key, error => error.Value))
            {
                Status = (int)statusCode,
                Title = statusCode.ToReasonPhrase(),
                Detail = definition.Message
            }
            : ApplicationProblemDetails.Create(statusCode, definition.Message);
}
