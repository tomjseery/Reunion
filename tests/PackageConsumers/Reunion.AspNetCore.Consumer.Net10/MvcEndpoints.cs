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
    public ActionResult<UserResponse> CreateProjectedAtUri(Result<User, UserError> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}");

    public ActionResult<User> Create(Result<User, UserError> result) =>
        result.ToCreatedOrProblem();

    public ActionResult<UserResponse> CreateProjected(
        Result<User, UserError> result) =>
        result.ToCreatedOrProblem(user => new UserResponse(user.Id));

    public ActionResult<User> Create(Result<User> result) =>
        result.ToCreatedOrProblem(ToProblem);

    public ActionResult<UserResponse> CreateProjected(Result<User> result) =>
        result.ToCreatedOrProblem(user => new UserResponse(user.Id), ToProblem);

    public ActionResult<User> Create(Result<User, ConsumerError> result) =>
        result.ToCreatedOrProblem(ToProblem);

    public ActionResult<UserResponse> CreateProjected(
        Result<User, ConsumerError> result) =>
        result.ToCreatedOrProblem(ToProblem, user => new UserResponse(user.Id));

    public ActionResult<User> CreateAtAction(Result<User, UserError> result) =>
        result.ToCreatedAtActionOrProblem(nameof(Get), new { id = 42 });

    public ActionResult<UserResponse> CreateProjectedAtAction(
        Result<User, UserError> result) =>
        result.ToCreatedAtActionOrProblem(
            user => new UserResponse(user.Id),
            nameof(Get),
            user => new { id = user.Id });

    public ActionResult<User> CreateAtAction(Result<User> result) =>
        result.ToCreatedAtActionOrProblem(nameof(Get), new { id = 42 }, ToProblem);

    public ActionResult<UserResponse> CreateProjectedAtAction(Result<User> result) =>
        result.ToCreatedAtActionOrProblem(
            user => new UserResponse(user.Id),
            nameof(Get),
            user => new { id = user.Id },
            ToProblem);

    public ActionResult<User> CreateAtAction(Result<User, ConsumerError> result) =>
        result.ToCreatedAtActionOrProblem(nameof(Get), new { id = 42 }, ToProblem);

    public ActionResult<UserResponse> CreateProjectedAtAction(
        Result<User, ConsumerError> result) =>
        result.ToCreatedAtActionOrProblem(
            user => new UserResponse(user.Id),
            nameof(Get),
            user => new { id = user.Id },
            ToProblem);

    public ActionResult<User> CreateAtRoute(Result<User, UserError> result) =>
        result.ToCreatedAtRouteOrProblem("user", new { id = 42 });

    public ActionResult<UserResponse> CreateProjectedAtRoute(
        Result<User, UserError> result) =>
        result.ToCreatedAtRouteOrProblem(
            user => new UserResponse(user.Id),
            "user",
            user => new { id = user.Id });

    public ActionResult<User> CreateAtRoute(Result<User> result) =>
        result.ToCreatedAtRouteOrProblem("user", new { id = 42 }, ToProblem);

    public ActionResult<UserResponse> CreateProjectedAtRoute(Result<User> result) =>
        result.ToCreatedAtRouteOrProblem(
            user => new UserResponse(user.Id),
            "user",
            user => new { id = user.Id },
            ToProblem);

    public ActionResult<User> CreateAtRoute(Result<User, ConsumerError> result) =>
        result.ToCreatedAtRouteOrProblem("user", new { id = 42 }, ToProblem);

    public ActionResult<UserResponse> CreateProjectedAtRoute(
        Result<User, ConsumerError> result) =>
        result.ToCreatedAtRouteOrProblem(
            user => new UserResponse(user.Id),
            "user",
            user => new { id = user.Id },
            ToProblem);

    public ActionResult<UserResponse> GetProjected(Result<User, UserError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id));

    public ActionResult<UserResponse> GetProjected(Result<User> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public ActionResult<UserResponse> CreateProjectedAtUri(Result<User> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}",
            ToProblem);

    public ActionResult<UserResponse> GetProjected(Result<User, ConsumerError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public ActionResult<UserResponse> CreateProjectedAtUri(Result<User, ConsumerError> result) =>
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


    private static void CompileAutomaticCreatedOverloads(Result<User, UserError> result)
    {
        ActionResult<User> actionOnly = result.ToCreatedAtActionOrProblem("Get");
        ActionResult<User> actionWithValues =
            result.ToCreatedAtActionOrProblem("Get", new { id = 42 });
        ActionResult<User> actionWithController =
            result.ToCreatedAtActionOrProblem("Get", new { id = 42 }, "Users");
        ActionResult<User> actionWithSelector =
            result.ToCreatedAtActionOrProblem("Get", user => new { id = user.Id });
        ActionResult<User> actionWithSelectorAndController =
            result.ToCreatedAtActionOrProblem(
                "Get",
                user => new { id = user.Id },
                "Users");
        ActionResult<UserResponse> projectedActionOnly =
            result.ToCreatedAtActionOrProblem(user => new UserResponse(user.Id), "Get");
        ActionResult<UserResponse> projectedActionWithValues =
            result.ToCreatedAtActionOrProblem(
                user => new UserResponse(user.Id),
                "Get",
                new { id = 42 });
        ActionResult<UserResponse> projectedActionWithController =
            result.ToCreatedAtActionOrProblem(
                user => new UserResponse(user.Id),
                "Get",
                new { id = 42 },
                "Users");
        ActionResult<UserResponse> projectedActionWithSelector =
            result.ToCreatedAtActionOrProblem(
                user => new UserResponse(user.Id),
                "Get",
                user => new { id = user.Id });
        ActionResult<UserResponse> projectedActionWithSelectorAndController =
            result.ToCreatedAtActionOrProblem(
                user => new UserResponse(user.Id),
                "Get",
                user => new { id = user.Id },
                "Users");

        ActionResult<User> routeOnly = result.ToCreatedAtRouteOrProblem();
        ActionResult<User> namedRoute = result.ToCreatedAtRouteOrProblem("user");
        ActionResult<User> routeWithValues =
            result.ToCreatedAtRouteOrProblem("user", new { id = 42 });
        ActionResult<User> routeWithSelector =
            result.ToCreatedAtRouteOrProblem("user", user => new { id = user.Id });
        ActionResult<UserResponse> projectedRouteOnly =
            result.ToCreatedAtRouteOrProblem(user => new UserResponse(user.Id));
        ActionResult<UserResponse> projectedNamedRoute =
            result.ToCreatedAtRouteOrProblem(user => new UserResponse(user.Id), "user");
        ActionResult<UserResponse> projectedRouteWithValues =
            result.ToCreatedAtRouteOrProblem(
                user => new UserResponse(user.Id),
                "user",
                new { id = 42 });
        ActionResult<UserResponse> projectedRouteWithSelector =
            result.ToCreatedAtRouteOrProblem(
                user => new UserResponse(user.Id),
                "user",
                user => new { id = user.Id });

        _ = actionOnly;
        _ = actionWithValues;
        _ = actionWithController;
        _ = actionWithSelector;
        _ = actionWithSelectorAndController;
        _ = projectedActionOnly;
        _ = projectedActionWithValues;
        _ = projectedActionWithController;
        _ = projectedActionWithSelector;
        _ = projectedActionWithSelectorAndController;
        _ = routeOnly;
        _ = namedRoute;
        _ = routeWithValues;
        _ = routeWithSelector;
        _ = projectedRouteOnly;
        _ = projectedNamedRoute;
        _ = projectedRouteWithValues;
        _ = projectedRouteWithSelector;
    }

    private static void CompileMappedActionControllerOverloads(
        Result<User> stringResult,
        Result<User, ConsumerError> genericResult)
    {
        ActionResult<User> stringMapped = stringResult.ToCreatedAtActionOrProblem(
            "Get",
            new { id = 42 },
            ToProblem,
            "Users");
        ActionResult<UserResponse> stringProjected =
            stringResult.ToCreatedAtActionOrProblem(
                user => new UserResponse(user.Id),
                "Get",
                user => new { id = user.Id },
                ToProblem,
                "Users");
        ActionResult<User> genericMapped = genericResult.ToCreatedAtActionOrProblem(
            "Get",
            new { id = 42 },
            ToProblem,
            "Users");
        ActionResult<UserResponse> genericProjected =
            genericResult.ToCreatedAtActionOrProblem(
                user => new UserResponse(user.Id),
                "Get",
                user => new { id = user.Id },
                ToProblem,
                "Users");

        _ = stringMapped;
        _ = stringProjected;
        _ = genericMapped;
        _ = genericProjected;
    }


    private static ProblemDetails ToProblem(string error) =>
        new() { Status = StatusCodes.Status400BadRequest, Detail = error };

    private static ProblemDetails ToProblem(ConsumerError error) =>
        new() { Status = StatusCodes.Status409Conflict, Detail = error.Code };
}
