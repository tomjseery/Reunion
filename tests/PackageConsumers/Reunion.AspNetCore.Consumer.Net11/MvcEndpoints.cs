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
    public ActionResult<User> Create(Result<User, UserError> result) =>
        result.ToActionResult(user => new CreatedResult($"/users/{user.Id}", user));
}
