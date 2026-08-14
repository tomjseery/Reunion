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
    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjectedAtUri(
        Result<User, UserError> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}");

    public Results<Created<User>, ProblemHttpResult> Create(
        Result<User, UserError> result) =>
        result.ToCreatedOrProblem();

    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjected(
        Result<User, UserError> result) =>
        result.ToCreatedOrProblem(user => new UserResponse(user.Id));

    public Results<Created<User>, ProblemHttpResult> Create(Result<User> result) =>
        result.ToCreatedOrProblem(ToProblem);

    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjected(
        Result<User> result) =>
        result.ToCreatedOrProblem(user => new UserResponse(user.Id), ToProblem);

    public Results<Created<User>, ProblemHttpResult> Create(
        Result<User, ConsumerError> result) =>
        result.ToCreatedOrProblem(ToProblem);

    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjected(
        Result<User, ConsumerError> result) =>
        result.ToCreatedOrProblem(ToProblem, user => new UserResponse(user.Id));

    public Results<CreatedAtRoute<User>, ProblemHttpResult> CreateAtRoute(
        Result<User, UserError> result) =>
        result.ToCreatedAtRouteOrProblem("user", new { id = 42 });

    public Results<CreatedAtRoute<UserResponse>, ProblemHttpResult> CreateProjectedAtRoute(
        Result<User, UserError> result) =>
        result.ToCreatedAtRouteOrProblem(
            user => new UserResponse(user.Id),
            "user",
            user => new { id = user.Id });

    public Results<CreatedAtRoute<User>, ProblemHttpResult> CreateAtRoute(Result<User> result) =>
        result.ToCreatedAtRouteOrProblem("user", new { id = 42 }, ToProblem);

    public Results<CreatedAtRoute<UserResponse>, ProblemHttpResult> CreateProjectedAtRoute(
        Result<User> result) =>
        result.ToCreatedAtRouteOrProblem(
            user => new UserResponse(user.Id),
            "user",
            user => new { id = user.Id },
            ToProblem);

    public Results<CreatedAtRoute<User>, ProblemHttpResult> CreateAtRoute(
        Result<User, ConsumerError> result) =>
        result.ToCreatedAtRouteOrProblem("user", new { id = 42 }, ToProblem);

    public Results<CreatedAtRoute<UserResponse>, ProblemHttpResult> CreateProjectedAtRoute(
        Result<User, ConsumerError> result) =>
        result.ToCreatedAtRouteOrProblem(
            user => new UserResponse(user.Id),
            "user",
            user => new { id = user.Id },
            ToProblem);

    public Results<Ok<UserResponse>, ProblemHttpResult> GetProjected(
        Result<User, UserError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id));

    public Results<Ok<UserResponse>, ProblemHttpResult> GetProjected(Result<User> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjectedAtUri(
        Result<User> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}",
            ToProblem);

    public Results<Ok<UserResponse>, ProblemHttpResult> GetProjected(
        Result<User, ConsumerError> result) =>
        result.ToOkOrProblem(user => new UserResponse(user.Id), ToProblem);

    public Results<Created<UserResponse>, ProblemHttpResult> CreateProjectedAtUri(
        Result<User, ConsumerError> result) =>
        result.ToCreatedOrProblem(
            user => new UserResponse(user.Id),
            user => $"/users/{user.Id}",
            ToProblem);


    private static void CompileAutomaticCreatedAtRouteOverloads(
        Result<User, UserError> result)
    {
        Results<CreatedAtRoute<User>, ProblemHttpResult> routeOnly =
            result.ToCreatedAtRouteOrProblem();
        Results<CreatedAtRoute<User>, ProblemHttpResult> namedRoute =
            result.ToCreatedAtRouteOrProblem("user");
        Results<CreatedAtRoute<User>, ProblemHttpResult> routeWithValues =
            result.ToCreatedAtRouteOrProblem("user", new { id = 42 });
        Results<CreatedAtRoute<User>, ProblemHttpResult> routeWithSelector =
            result.ToCreatedAtRouteOrProblem("user", user => new { id = user.Id });
        Results<CreatedAtRoute<UserResponse>, ProblemHttpResult> projectedRouteOnly =
            result.ToCreatedAtRouteOrProblem(user => new UserResponse(user.Id));
        Results<CreatedAtRoute<UserResponse>, ProblemHttpResult> projectedNamedRoute =
            result.ToCreatedAtRouteOrProblem(user => new UserResponse(user.Id), "user");
        Results<CreatedAtRoute<UserResponse>, ProblemHttpResult> projectedRouteWithValues =
            result.ToCreatedAtRouteOrProblem(
                user => new UserResponse(user.Id),
                "user",
                new { id = 42 });
        Results<CreatedAtRoute<UserResponse>, ProblemHttpResult> projectedRouteWithSelector =
            result.ToCreatedAtRouteOrProblem(
                user => new UserResponse(user.Id),
                "user",
                user => new { id = user.Id });

        _ = routeOnly;
        _ = namedRoute;
        _ = routeWithValues;
        _ = routeWithSelector;
        _ = projectedRouteOnly;
        _ = projectedNamedRoute;
        _ = projectedRouteWithValues;
        _ = projectedRouteWithSelector;
    }


    private static ProblemDetails ToProblem(string error) =>
        new() { Status = StatusCodes.Status400BadRequest, Detail = error };

    private static ProblemDetails ToProblem(ConsumerError error) =>
        new() { Status = StatusCodes.Status409Conflict, Detail = error.Code };
}
