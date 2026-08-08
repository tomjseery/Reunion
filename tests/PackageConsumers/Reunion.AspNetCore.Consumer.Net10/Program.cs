using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reunion;

var user = new User(42);
Option<User> found = Option.Some(user);
Result<User, DomainError> created = Result.Success<User, DomainError>(user);

var httpController = new HttpResultEndpoints();
var mvcController = new MvcEndpoints();
var httpGet = httpController.Get(found);
var httpPost = httpController.Create(created);
var mvcGet = mvcController.Get(found);
var mvcPost = mvcController.Create(created);

Require(httpGet.Result is Ok<User>, "The HttpResults GET mapping did not return Ok<User>.");
Require(httpPost.Result is Created<User> httpCreated && httpCreated.Location == "/users/42", "The HttpResults POST mapping did not preserve Location.");
Require(mvcGet.Result is OkObjectResult, "The MVC GET mapping did not return OkObjectResult.");
Require(mvcPost.Result is CreatedResult mvcCreated && mvcCreated.Location == "/users/42", "The MVC POST mapping did not preserve Location.");

Console.WriteLine("Reunion.AspNetCore net10 package consumer passed.");

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

public sealed record User(int Id);

public sealed record DomainError(string Code);
