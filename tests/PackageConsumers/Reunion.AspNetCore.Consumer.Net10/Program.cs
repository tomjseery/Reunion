using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reunion;
using Reunion.AspNetCore;
using Reunion.Errors;

var user = new User(42);
Option<User> found = Option.Some(user);
Result<User, DomainError> created = Result.Success<User, DomainError>(user);
var missingError = new DomainError(
    ErrorDefinition.NotFound("user.not_found", "User not found."));
Result<User, DomainError> missing = Result.Failure<User, DomainError>(missingError);

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
Require(httpPost.Result is Created<User> httpCreated && httpCreated.Location == "/users/42", "The HttpResults POST mapping did not preserve Location.");
Require(mvcGet.Result is OkObjectResult, "The MVC GET mapping did not return OkObjectResult.");
Require(mvcPost.Result is CreatedResult mvcCreated && mvcCreated.Location == "/users/42", "The MVC POST mapping did not preserve Location.");
Require(
    httpProblem?.StatusCode == StatusCodes.Status404NotFound
    && httpProblem.ProblemDetails.Extensions.TryGetValue(
        ErrorProblemDetails.CodeExtensionKey,
        out var httpCode)
    && Equals(httpCode, "user.not_found"),
    "The HttpResults typed error did not use the automatic not-found problem mapping.");
Require(
    mvcProblem?.StatusCode == StatusCodes.Status404NotFound
    && mvcDetails?.Extensions.TryGetValue(
        ErrorProblemDetails.CodeExtensionKey,
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

public sealed record DomainError(ErrorDefinition Definition) : IError;
