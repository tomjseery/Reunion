using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reunion;

var user = new User(42);
Option<User> found = new Some<User>(user);
Result<User, DomainError> created = new Success<User>(user);

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
Require(Match(created) == 42, "The transitive Reunion net11 asset did not retain native union matching.");

Console.WriteLine("Reunion.AspNetCore net11 package consumer passed.");

static int Match(Result<User, DomainError> result) => result switch
{
    Success<User>(var value) => value.Id,
    Failure<DomainError> _ => -1
};

static void Require(bool condition, string message)
{
    if (!condition)
        throw new InvalidOperationException(message);
}

public sealed record User(int Id);

public sealed record DomainError(string Code);
