using Microsoft.AspNetCore.Http;
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
    public ActionResult<User> Create(Result<User, DomainError> result) =>
        result.ToCreatedOrProblem(
            user => $"/users/{user.Id}",
            error => new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Detail = error.Code
            });
}
