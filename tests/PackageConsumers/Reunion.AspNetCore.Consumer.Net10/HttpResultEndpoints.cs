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
    public Results<Created<UserResponse>, ProblemHttpResult> Create(
        Result<User, UserError> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}");

    public Results<Ok<UserResponse>, ProblemHttpResult> GetProjected(
        Result<User, UserError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id));

    public Results<Ok<UserResponse>, ProblemHttpResult> GetProjected(Result<User> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjected(
        Result<User> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}",
            ToProblem);

    public Results<Ok<UserResponse>, ProblemHttpResult> GetProjected(
        Result<User, ConsumerError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjected(
        Result<User, ConsumerError> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}",
            ToProblem);

    private static ProblemDetails ToProblem(string error) =>
        new() { Status = StatusCodes.Status400BadRequest, Detail = error };

    private static ProblemDetails ToProblem(ConsumerError error) =>
        new() { Status = StatusCodes.Status409Conflict, Detail = error.Code };
}
