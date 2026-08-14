using Microsoft.AspNetCore.Mvc;
using Reunion;
using Reunion.AspNetCore.Mvc;

[ApiController]
[Route("mvc-results")]
public sealed class MvcEndpoints : ControllerBase
{
    [HttpGet]
    public ActionResult<User> Get(Option<User> result) =>
        result.ToOkOrNotFound();

    [HttpPost]
    public ActionResult<UserResponse> Create(Result<User, UserError> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}");

    public ActionResult<UserResponse> GetProjected(Result<User, UserError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id));

    public ActionResult<UserResponse> GetProjected(Result<User> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public ActionResult<UserResponse> CreateProjected(Result<User> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}",
            ToProblem);

    public ActionResult<UserResponse> GetProjected(Result<User, ConsumerError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public ActionResult<UserResponse> CreateProjected(Result<User, ConsumerError> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}",
            ToProblem);

    public ActionResult<UserResponse> ToProjectedAction(Result<User, UserError> result)
    {
        Func<User, ActionResult<UserResponse>> successMapper = user =>
            new OkObjectResult(new UserResponse(user.Id));
        return result.ToActionResult(successMapper);
    }

    private static ProblemDetails ToProblem(string error) =>
        new() { Status = StatusCodes.Status400BadRequest, Detail = error };

    private static ProblemDetails ToProblem(ConsumerError error) =>
        new() { Status = StatusCodes.Status409Conflict, Detail = error.Code };
}
