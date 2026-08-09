using System.Net;
using Microsoft.AspNetCore.WebUtilities;

namespace Reunion.AspNetCore;

internal static class HttpStatusCodeExtensions
{
    internal static string ToReasonPhrase(this HttpStatusCode statusCode) =>
        ReasonPhrases.GetReasonPhrase((int)statusCode);
}
