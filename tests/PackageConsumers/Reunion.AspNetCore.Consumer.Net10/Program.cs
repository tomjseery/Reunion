using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reunion;
using Reunion.AspNetCore;
using Reunion.Errors;

var user = new User(42);
Option<User> found = Option.Some(user);
Result<User, UserError> created = Result.Success<User, UserError>(user);
var missingError = new UserError(ErrorDefinition.NotFound<UserError.UserNotFound>());
Result<User, UserError> missing = Result.Failure<User, UserError>(missingError);
var badRequest = ProblemDetails.Create(HttpStatusCode.BadRequest, "The request is invalid.");
var missingDetails = ProblemDetails.Create(missingError);

var httpController = new HttpResultEndpoints();
var mvcController = new MvcEndpoints();
var httpGet = httpController.Get(found);
var httpPost = httpController.Create(created);
var mvcGet = mvcController.Get(found);
var mvcPost = mvcController.Create(created);
var httpMissing = httpController.Create(missing);
var mvcMissing = mvcController.Create(missing);
var httpProblem = httpMissing.Result as ProblemHttpResult;
var mvcProblem = mvcMissing.Result as ObjectResult;
var mvcDetails = mvcProblem?.Value as ProblemDetails;

Require(httpGet.Result is Ok<User>, "The HttpResults GET mapping did not return Ok<User>.");
Require(
    httpPost.Result is Created<UserResponse> { Location: "/users/42", Value.Id: 42 },
    "The HttpResults projected POST mapping did not preserve its response or Location.");
Require(mvcGet.Result is OkObjectResult, "The MVC GET mapping did not return OkObjectResult.");
Require(
    badRequest is { Status: StatusCodes.Status400BadRequest, Title: "Bad Request" }
    && missingDetails.Status == StatusCodes.Status404NotFound,
    "The ProblemDetails factories did not preserve their HTTP mappings.");
Require(
    mvcPost.Result is CreatedResult { Location: "/users/42", Value: UserResponse { Id: 42 } },
    "The MVC projected POST mapping did not preserve its response or Location.");
Require(
    httpProblem?.StatusCode == StatusCodes.Status404NotFound
    && httpProblem.ProblemDetails.Extensions.TryGetValue(
        ProblemDetailsExtensions.CodeExtensionKey,
        out var httpCode)
    && Equals(httpCode, "user.not_found"),
    "The HttpResults typed error did not use the automatic not-found problem mapping.");
Require(
    mvcProblem?.StatusCode == StatusCodes.Status404NotFound
    && mvcDetails?.Extensions.TryGetValue(
        ProblemDetailsExtensions.CodeExtensionKey,
        out var mvcCode) == true
    && Equals(mvcCode, "user.not_found"),
    "The MVC typed error did not use the automatic not-found problem mapping.");

Console.WriteLine("Reunion.AspNetCore net10 package consumer passed.");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

public sealed record User(int Id);

public sealed record UserResponse(int Id);

public sealed record ConsumerError(string Code);

public sealed record UserError(ErrorDefinition Definition) : IError
{
    public sealed record UserNotFound;
}
