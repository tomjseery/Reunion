using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reunion;
using Reunion.AspNetCore.HttpResults;

[ApiController]
[Route("http-results")]
public sealed class HttpResultEndpoints : ControllerBase
{
    [HttpGet]
    public Results<Ok<User>, NotFound> Get(Option<User> result) =>
        result.ToOkOrNotFound();

    [HttpPost]
    public Results<Created<User>, ProblemHttpResult> Create(
        Result<User, DomainError> result) =>
        result.ToCreatedOrProblem(
            user => $"/users/{user.Id}",
            error => TypedResults.Problem(
                detail: error.Code,
                statusCode: StatusCodes.Status400BadRequest));
}
