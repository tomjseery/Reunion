using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Reunion.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore.Tests;

public sealed class MvcResultExtensionsTests
{
    [Fact]
    public void OptionMappings_EveryCase_ReturnsExactMvcResult()
    {
        ActionResult<User> found = Option.Some(new User(42)).ToOkOrNotFound();
        ActionResult<User> missing = Option.None<User>().ToOkOrNotFound();
        ActionResult<User> present = Option.Some(new User(43)).ToOkOrNoContent();
        ActionResult<User> absent = Option.None<User>().ToOkOrNoContent();
        ActionResult<string> projectedFound = Option.Some(new User(44))
            .ToOkOrNotFound(user => user.Id.ToString());
        ActionResult<string> projectedAbsent = Option.None<User>()
            .ToOkOrNoContent(user => user.Id.ToString());

        var foundResult = Assert.IsType<OkObjectResult>(found.Result);
        Assert.Equal(StatusCodes.Status200OK, foundResult.StatusCode);
        Assert.Equal(42, Assert.IsType<User>(foundResult.Value).Id);
        Assert.IsType<NotFoundResult>(missing.Result);
        Assert.Equal(43, Assert.IsType<User>(Assert.IsType<OkObjectResult>(present.Result).Value).Id);
        Assert.IsType<NoContentResult>(absent.Result);
        Assert.Equal(
            "44",
            Assert.IsType<string>(Assert.IsType<OkObjectResult>(projectedFound.Result).Value));
        Assert.IsType<NoContentResult>(projectedAbsent.Result);
    }

    [Fact]
    public void ToOkOr_MapsAbsenceWithCallerSelectedMvcResult()
    {
        var controller = new TestController();
        ActionResult<User> found = controller.ToUnauthorized(Option.Some(new User(42)));
        ActionResult<User> missing = controller.ToUnauthorized(Option.None<User>());
        ActionResult<string> projected = controller.ToConflict(Option.Some(new User(43)));

        Assert.Equal(
            42,
            Assert.IsType<User>(Assert.IsType<OkObjectResult>(found.Result).Value).Id);
        Assert.IsType<UnauthorizedResult>(missing.Result);
        Assert.Equal(
            "43",
            Assert.IsType<string>(Assert.IsType<OkObjectResult>(projected.Result).Value));
    }

    [Fact]
    public void ToOkOr_ValidatesDelegatesAndInvokesOnlySelectedBranch()
    {
        var projectionInvocations = 0;
        var alternativeInvocations = 0;
        Func<User, string> projection = user =>
        {
            projectionInvocations++;
            return user.Id.ToString();
        };
        Func<ActionResult> alternative = () =>
        {
            alternativeInvocations++;
            return new UnauthorizedResult();
        };

        Option.Some(new User(42)).ToOkOr(projection, alternative);
        Assert.Equal(1, projectionInvocations);
        Assert.Equal(0, alternativeInvocations);

        Option.None<User>().ToOkOr(projection, alternative);
        Assert.Equal(1, projectionInvocations);
        Assert.Equal(1, alternativeInvocations);

        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42)).ToOkOr(null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42)).ToOkOr<User, string>(null!, alternative));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42)).ToOkOr(projection, null!));
        Assert.Throws<InvalidOperationException>(() =>
            Option.None<User>().ToOkOr(() => null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42)).ToOkOr(_ => (string)null!, alternative));
    }

    [Fact]
    public void StringResultMappings_RequireMapperAndDoNotExposeError()
    {
        ActionResult ok = Result.Success().ToOkOrProblem(ToInternalServerProblem);
        ActionResult failed = Result.Failure("operation failed").ToOkOrProblem(ToInternalServerProblem);
        ActionResult noContent = Result.Success().ToNoContentOrProblem(ToInternalServerProblem);
        ActionResult noContentFailed = Result.Failure("delete failed")
            .ToNoContentOrProblem(ToInternalServerProblem);

        Assert.IsType<OkResult>(ok);
        AssertMvcProblem(failed, StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
        Assert.IsType<NoContentResult>(noContent);
        AssertMvcProblem(
            noContentFailed,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.");
    }

    [Fact]
    public void ValueResultMappings_EveryCase_ReturnsExpectedMvcResult()
    {
        var user = new User(42);
        ActionResult<User> ok = Result.Success(user).ToOkOrProblem(ToInternalServerProblem);
        ActionResult<User> failed = Result.Failure<User>("missing")
            .ToOkOrProblem(ToInternalServerProblem);
        ActionResult<User> created =
            Result.Success(user).ToCreatedOrProblem(
                value => $"/users/{value.Id}",
                ToInternalServerProblem);
        ActionResult<User> createFailed =
            Result.Failure<User>("invalid").ToCreatedOrProblem(
                value => $"/users/{value.Id}",
                ToInternalServerProblem);

        Assert.Same(user, Assert.IsType<OkObjectResult>(ok.Result).Value);
        AssertMvcProblem(
            failed.Result!,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.");

        var createdResult = Assert.IsType<CreatedResult>(created.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal("/users/42", createdResult.Location);
        Assert.Same(user, createdResult.Value);
        AssertMvcProblem(
            createFailed.Result!,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.");
    }

    [Fact]
    public void GenericResultMappings_EveryCase_PreservesStructuredProblemDetails()
    {
        var user = new User(42);
        var error = new DomainError("user.missing");
        var details = new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                ["id"] = ["The user does not exist."]
            })
        {
            Status = StatusCodes.Status404NotFound,
            Title = "User not found",
            Detail = "The requested user does not exist.",
            Extensions = { ["code"] = error.Code }
        };
        Func<DomainError, ProblemDetails> mapper = _ => details;

        ActionResult<User> ok = Result.Success<User, DomainError>(user).ToOkOrProblem(mapper);
        ActionResult<User> failed = Result.Failure<User, DomainError>(error).ToOkOrProblem(mapper);
        ActionResult<User> created = Result.Success<User, DomainError>(user)
            .ToCreatedOrProblem(value => $"/users/{value.Id}", mapper);
        ActionResult<User> createFailed = Result.Failure<User, DomainError>(error)
            .ToCreatedOrProblem(value => $"/users/{value.Id}", mapper);

        Assert.Same(user, Assert.IsType<OkObjectResult>(ok.Result).Value);
        AssertSameProblem(details, failed.Result);
        Assert.Equal("/users/42", Assert.IsType<CreatedResult>(created.Result).Location);
        AssertSameProblem(details, createFailed.Result);
    }

    [Fact]
    public void UnitResultMappings_EveryCase_ReturnsExpectedMvcResult()
    {
        var error = new DomainError("delete.denied");
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Detail = "Delete denied."
        };
        Func<DomainError, ProblemDetails> mapper = _ => details;

        ActionResult ok = UnitResult.Success<DomainError>().ToOkOrProblem(mapper);
        ActionResult failed = UnitResult.Failure(error).ToOkOrProblem(mapper);
        ActionResult noContent = UnitResult.Success<DomainError>().ToNoContentOrProblem(mapper);
        ActionResult noContentFailed = UnitResult.Failure(error).ToNoContentOrProblem(mapper);

        Assert.IsType<OkResult>(ok);
        AssertSameProblem(details, failed);
        Assert.IsType<NoContentResult>(noContent);
        AssertSameProblem(details, noContentFailed);
    }

    [Fact]
    public void TypedApplicationErrors_MapWithoutCallerMapper()
    {
        var error = new ApplicationError(
            ErrorDefinition.NotFound("user.not_found", "User not found."));

        var result = Result.Failure<User, ApplicationError>(error).ToOkOrProblem();

        var objectResult = Assert.IsAssignableFrom<ObjectResult>(result.Result);
        var details = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(StatusCodes.Status404NotFound, objectResult.StatusCode);
        Assert.Equal("User not found.", details.Detail);
        Assert.Equal("user.not_found", details.Extensions["code"]);
    }

    [Fact]
    public void LocationlessCreatedMappings_CoverEveryErrorShapeAndProjection()
    {
        var user = new User(42);
        var applicationError = new ApplicationError(
            ErrorDefinition.Conflict("user.conflict", "User conflict."));
        var domainError = new DomainError("user.conflict");
        var domainDetails = new ProblemDetails { Status = StatusCodes.Status409Conflict };
        var projectionInvocations = 0;
        var mapperInvocations = 0;
        Func<User, UserResponse> projection = value =>
        {
            projectionInvocations++;
            return new(value.Id);
        };
        Func<DomainError, ProblemDetails> domainMapper = _ =>
        {
            mapperInvocations++;
            return domainDetails;
        };
        Func<string, ProblemDetails> stringMapper = _ =>
        {
            mapperInvocations++;
            return new() { Status = StatusCodes.Status400BadRequest };
        };

        ActionResult<User> automatic = Result.Success<User, ApplicationError>(user)
            .ToCreatedOrProblem();
        ActionResult<UserResponse> automaticProjected =
            Result.Success<User, ApplicationError>(user).ToCreatedOrProblem(projection);
        ActionResult<UserResponse> automaticFailed =
            Result.Failure<User, ApplicationError>(applicationError)
                .ToCreatedOrProblem(projection);
        ActionResult<User> stringCreated = Result.Success(user)
            .ToCreatedOrProblem(stringMapper);
        ActionResult<UserResponse> stringProjected = Result.Success(user)
            .ToCreatedOrProblem(projection, stringMapper);
        ActionResult<UserResponse> stringFailed = Result.Failure<User>("failed")
            .ToCreatedOrProblem(projection, stringMapper);
        ActionResult<User> genericCreated = Result.Success<User, DomainError>(user)
            .ToCreatedOrProblem(domainMapper);
        ActionResult<UserResponse> genericFailed = Result.Failure<User, DomainError>(domainError)
            .ToCreatedOrProblem(domainMapper, projection);

        AssertLocationlessCreated(automatic, user);
        AssertLocationlessCreated(automaticProjected, new UserResponse(42));
        AssertMvcProblem(automaticFailed.Result!, StatusCodes.Status409Conflict, "User conflict.");
        AssertLocationlessCreated(stringCreated, user);
        AssertLocationlessCreated(stringProjected, new UserResponse(42));
        AssertMvcProblem(stringFailed.Result!, StatusCodes.Status400BadRequest, null);
        AssertLocationlessCreated(genericCreated, user);
        AssertSameProblem(domainDetails, genericFailed.Result);
        Assert.Equal(2, projectionInvocations);
        Assert.Equal(2, mapperInvocations);
    }

    [Fact]
    public void LocationlessCreatedMappings_ValidateProjections()
    {
        var applicationResult = Result.Success<User, ApplicationError>(new(42));
        var stringResult = Result.Success(new User(42));
        var genericResult = Result.Success<User, DomainError>(new(42));
        Func<string, ProblemDetails> stringMapper = ToInternalServerProblem;
        Func<DomainError, ProblemDetails> genericMapper = _ =>
            new() { Status = StatusCodes.Status400BadRequest };

        Assert.Throws<ArgumentNullException>(() =>
            applicationResult.ToCreatedOrProblem<User, ApplicationError, UserResponse>(null!));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToCreatedOrProblem<User, UserResponse>(null!, stringMapper));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToCreatedOrProblem<User, DomainError, UserResponse>(genericMapper, null!));
        Assert.Throws<ArgumentNullException>(() =>
            applicationResult.ToCreatedOrProblem(_ => (UserResponse)null!));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToCreatedOrProblem(_ => (UserResponse)null!, stringMapper));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToCreatedOrProblem(genericMapper, _ => (UserResponse)null!));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToCreatedOrProblem((Func<string, ProblemDetails>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToCreatedOrProblem((Func<DomainError, ProblemDetails>)null!));
    }

    [Fact]
    public void CreatedAtActionAndRouteMappings_CoverEveryErrorShapeAndProjection()
    {
        var user = new User(42);
        var applicationError = new ApplicationError(
            ErrorDefinition.Conflict("user.conflict", "User conflict."));
        var routeSelectorInvocations = 0;
        var projectionInvocations = 0;
        var mapperInvocations = 0;
        Func<User, object?> routeValuesSelector = value =>
        {
            routeSelectorInvocations++;
            return new { id = value.Id };
        };
        Func<User, UserResponse> projection = value =>
        {
            projectionInvocations++;
            return new(value.Id);
        };
        Func<string, ProblemDetails> stringMapper = _ =>
        {
            mapperInvocations++;
            return new() { Status = StatusCodes.Status400BadRequest };
        };
        Func<DomainError, ProblemDetails> domainMapper = _ =>
        {
            mapperInvocations++;
            return new() { Status = StatusCodes.Status409Conflict };
        };

        ActionResult<User> atAction = Result.Success<User, ApplicationError>(user)
            .ToCreatedAtActionOrProblem("Get", new { id = 42 }, "Users");
        ActionResult<UserResponse> projectedAtAction =
            Result.Success<User, ApplicationError>(user)
                .ToCreatedAtActionOrProblem(projection, "Get", routeValuesSelector);
        ActionResult<UserResponse> failedAtAction =
            Result.Failure<User, ApplicationError>(applicationError)
                .ToCreatedAtActionOrProblem(projection, "Get", routeValuesSelector);
        ActionResult<User> mappedAtAction = Result.Success(user)
            .ToCreatedAtActionOrProblem("Get", new { id = 42 }, stringMapper);
        ActionResult<User> failedMappedAtAction = Result.Failure<User, DomainError>(new("failed"))
            .ToCreatedAtActionOrProblem("Get", new { id = 42 }, domainMapper);

        ActionResult<User> atRoute = Result.Success<User, ApplicationError>(user)
            .ToCreatedAtRouteOrProblem("user", new { id = 42 });
        ActionResult<UserResponse> projectedAtRoute =
            Result.Success<User, DomainError>(user)
                .ToCreatedAtRouteOrProblem(projection, "user", routeValuesSelector, domainMapper);
        ActionResult<UserResponse> failedAtRoute = Result.Failure<User>("failed")
            .ToCreatedAtRouteOrProblem(
                projection,
                "user",
                routeValuesSelector,
                stringMapper);

        var actionResult = Assert.IsType<CreatedAtActionResult>(atAction.Result);
        Assert.Equal("Get", actionResult.ActionName);
        Assert.Equal("Users", actionResult.ControllerName);
        Assert.Equal(42, actionResult.RouteValues!["id"]);
        Assert.Same(user, actionResult.Value);
        Assert.Equal(
            new UserResponse(42),
            Assert.IsType<CreatedAtActionResult>(projectedAtAction.Result).Value);
        var projectedActionResult = Assert.IsType<CreatedAtActionResult>(projectedAtAction.Result);
        Assert.Equal("Get", projectedActionResult.ActionName);
        Assert.Equal(42, projectedActionResult.RouteValues!["id"]);
        AssertMvcProblem(failedAtAction.Result!, StatusCodes.Status409Conflict, "User conflict.");
        Assert.Same(user, Assert.IsType<CreatedAtActionResult>(mappedAtAction.Result).Value);
        Assert.Equal(
            StatusCodes.Status409Conflict,
            Assert.IsAssignableFrom<ObjectResult>(failedMappedAtAction.Result).StatusCode);

        var routeResult = Assert.IsType<CreatedAtRouteResult>(atRoute.Result);
        Assert.Equal("user", routeResult.RouteName);
        Assert.Equal(42, routeResult.RouteValues!["id"]);
        Assert.Same(user, routeResult.Value);
        Assert.Equal(
            new UserResponse(42),
            Assert.IsType<CreatedAtRouteResult>(projectedAtRoute.Result).Value);
        var projectedRouteResult = Assert.IsType<CreatedAtRouteResult>(projectedAtRoute.Result);
        Assert.Equal("user", projectedRouteResult.RouteName);
        Assert.Equal(42, projectedRouteResult.RouteValues!["id"]);
        AssertMvcProblem(
            failedAtRoute.Result!,
            StatusCodes.Status400BadRequest,
            null);
        Assert.Equal(2, projectionInvocations);
        Assert.Equal(2, routeSelectorInvocations);
        Assert.Equal(2, mapperInvocations);
    }

    [Fact]
    public void CreatedAtActionAndRouteMappings_ValidateInputs()
    {
        var result = Result.Success<User, ApplicationError>(new(42));
        var stringResult = Result.Success(new User(42));
        var genericResult = Result.Success<User, DomainError>(new(42));
        Func<string, ProblemDetails> stringMapper = ToInternalServerProblem;
        Func<DomainError, ProblemDetails> genericMapper = _ =>
            new() { Status = StatusCodes.Status400BadRequest };

        Assert.Throws<ArgumentException>(() =>
            result.ToCreatedAtActionOrProblem(" "));
        Assert.Throws<ArgumentNullException>(() =>
            result.ToCreatedAtActionOrProblem("Get", (Func<User, object?>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            result.ToCreatedAtActionOrProblem<User, ApplicationError, UserResponse>(
                null!,
                "Get"));
        Assert.Throws<ArgumentNullException>(() =>
            result.ToCreatedAtRouteOrProblem("user", (Func<User, object?>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            result.ToCreatedAtRouteOrProblem<User, ApplicationError, UserResponse>(
                null!,
                "user"));
        Assert.Throws<ArgumentNullException>(() =>
            result.ToCreatedAtRouteOrProblem(_ => (UserResponse)null!, "user"));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToCreatedAtActionOrProblem(
                "Get",
                new { id = 42 },
                (Func<string, ProblemDetails>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToCreatedAtRouteOrProblem(
                "user",
                new { id = 42 },
                (Func<DomainError, ProblemDetails>)null!));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToCreatedAtRouteOrProblem(
                _ => (UserResponse)null!,
                "user",
                new { id = 42 },
                stringMapper));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToCreatedAtActionOrProblem(
                _ => (UserResponse)null!,
                "Get",
                new { id = 42 },
                genericMapper));
    }

    [Fact]
    public void CreatedConvenienceOverloads_InferProjectedStaticTypes()
    {
        var result = Result.Success<User, ApplicationError>(new(42));

        ActionResult<User> action = result.ToCreatedAtActionOrProblem("Get");
        ActionResult<UserResponse> projectedAction = result.ToCreatedAtActionOrProblem(
            user => new UserResponse(user.Id),
            "Get");
        ActionResult<User> route = result.ToCreatedAtRouteOrProblem();
        ActionResult<UserResponse> projectedRoute = result.ToCreatedAtRouteOrProblem(
            user => new UserResponse(user.Id));

        Assert.IsType<CreatedAtActionResult>(action.Result);
        Assert.IsType<CreatedAtActionResult>(projectedAction.Result);
        Assert.IsType<CreatedAtRouteResult>(route.Result);
        Assert.IsType<CreatedAtRouteResult>(projectedRoute.Result);
    }

    [Fact]
    public void TypedApplicationErrors_ToActionResult_DispatchesCustomSuccessAndFailure()
    {
        var user = new User(42);
        var error = new ApplicationError(
            ErrorDefinition.Conflict("user.conflict", "User conflict."));
        Func<User, ActionResult<User>> successMapper = value =>
            new AcceptedResult($"/jobs/{value.Id}", value);

        var accepted = Result.Success<User, ApplicationError>(user)
            .ToActionResult(successMapper);
        var failed = Result.Failure<User, ApplicationError>(error)
            .ToActionResult(successMapper);
        var completed = UnitResult.Success<ApplicationError>()
            .ToActionResult(() => new AcceptedResult());
        var completionFailed = UnitResult.Failure(error)
            .ToActionResult(() => new AcceptedResult());

        Assert.Equal("/jobs/42", Assert.IsType<AcceptedResult>(accepted.Result).Location);
        Assert.Equal(
            StatusCodes.Status409Conflict,
            Assert.IsAssignableFrom<ObjectResult>(failed.Result).StatusCode);
        Assert.IsType<AcceptedResult>(completed);
        Assert.Equal(
            StatusCodes.Status409Conflict,
            Assert.IsAssignableFrom<ObjectResult>(completionFailed).StatusCode);
    }

    [Fact]
    public void TypedApplicationErrors_ToActionResult_RejectsNullSuccessResults()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Result.Success<User, ApplicationError>(new User(42))
                .ToActionResult<User, ApplicationError, User>(_ => null!));
        Assert.Throws<InvalidOperationException>(() =>
            UnitResult.Success<ApplicationError>()
                .ToActionResult(() => null!));
    }

    [Fact]
    public void ProjectedTypedErrorTerminals_ReturnProjectedTypesAndRunOnlySuccessDelegates()
    {
        var user = new User(42);
        var error = new ApplicationError(
            ErrorDefinition.NotFound("user.not_found", "User not found."));
        var projectionInvocations = 0;
        var locationInvocations = 0;
        Func<User, UserResponse> projection = value =>
        {
            projectionInvocations++;
            return new UserResponse(value.Id);
        };
        Func<User, string> locationSelector = value =>
        {
            locationInvocations++;
            return $"/users/{value.Id}";
        };

        ActionResult<UserResponse> ok = Result.Success<User, ApplicationError>(user)
            .ToOkOrProblem(projection);
        ActionResult<UserResponse> failed = Result.Failure<User, ApplicationError>(error)
            .ToOkOrProblem(projection);
        ActionResult<UserResponse> created = Result.Success<User, ApplicationError>(user)
            .ToCreatedOrProblem(projection, locationSelector);
        ActionResult<UserResponse> createFailed = Result.Failure<User, ApplicationError>(error)
            .ToCreatedOrProblem(projection, locationSelector);

        Assert.Equal(42, Assert.IsType<UserResponse>(
            Assert.IsType<OkObjectResult>(ok.Result).Value).Id);
        AssertMvcProblem(failed.Result!, StatusCodes.Status404NotFound, "User not found.");
        var createdResult = Assert.IsType<CreatedResult>(created.Result);
        Assert.Equal("/users/42", createdResult.Location);
        Assert.Equal(42, Assert.IsType<UserResponse>(createdResult.Value).Id);
        AssertMvcProblem(createFailed.Result!, StatusCodes.Status404NotFound, "User not found.");
        Assert.Equal(2, projectionInvocations);
        Assert.Equal(1, locationInvocations);
    }

    [Fact]
    public void ProjectedStringErrorTerminals_ReturnProjectedTypesAndRunOnlySelectedDelegates()
    {
        var user = new User(42);
        var projectionInvocations = 0;
        var locationInvocations = 0;
        var mapperInvocations = 0;
        Func<User, UserResponse> projection = value =>
        {
            projectionInvocations++;
            return new UserResponse(value.Id);
        };
        Func<User, string> locationSelector = value =>
        {
            locationInvocations++;
            return $"/users/{value.Id}";
        };
        Func<string, ProblemDetails> errorMapper = error =>
        {
            mapperInvocations++;
            return new ProblemDetails { Status = 409, Detail = error };
        };

        ActionResult<UserResponse> ok = Result.Success(user)
            .ToOkOrProblem(projection, errorMapper);
        ActionResult<UserResponse> failed = Result.Failure<User>("missing")
            .ToOkOrProblem(projection, errorMapper);
        ActionResult<UserResponse> created = Result.Success(user)
            .ToCreatedOrProblem(projection, locationSelector, errorMapper);
        ActionResult<UserResponse> createFailed = Result.Failure<User>("invalid")
            .ToCreatedOrProblem(projection, locationSelector, errorMapper);

        Assert.Equal(42, Assert.IsType<UserResponse>(
            Assert.IsType<OkObjectResult>(ok.Result).Value).Id);
        AssertMvcProblem(failed.Result!, StatusCodes.Status409Conflict, "missing");
        var createdResult = Assert.IsType<CreatedResult>(created.Result);
        Assert.Equal("/users/42", createdResult.Location);
        Assert.Equal(42, Assert.IsType<UserResponse>(createdResult.Value).Id);
        AssertMvcProblem(createFailed.Result!, StatusCodes.Status409Conflict, "invalid");
        Assert.Equal(2, projectionInvocations);
        Assert.Equal(1, locationInvocations);
        Assert.Equal(2, mapperInvocations);
    }

    [Fact]
    public void ProjectedGenericErrorTerminals_ReturnProjectedTypesAndRunOnlySelectedDelegates()
    {
        var user = new User(42);
        var error = new DomainError("user.missing");
        var projectionInvocations = 0;
        var locationInvocations = 0;
        var mapperInvocations = 0;
        Func<User, UserResponse> projection = value =>
        {
            projectionInvocations++;
            return new UserResponse(value.Id);
        };
        Func<User, string> locationSelector = value =>
        {
            locationInvocations++;
            return $"/users/{value.Id}";
        };
        Func<DomainError, ProblemDetails> errorMapper = value =>
        {
            mapperInvocations++;
            return new ProblemDetails { Status = 404, Detail = value.Code };
        };

        ActionResult<UserResponse> ok = Result.Success<User, DomainError>(user)
            .ToOkOrProblem(projection, errorMapper);
        ActionResult<UserResponse> failed = Result.Failure<User, DomainError>(error)
            .ToOkOrProblem(projection, errorMapper);
        ActionResult<UserResponse> created = Result.Success<User, DomainError>(user)
            .ToCreatedOrProblem(projection, locationSelector, errorMapper);
        ActionResult<UserResponse> createFailed = Result.Failure<User, DomainError>(error)
            .ToCreatedOrProblem(projection, locationSelector, errorMapper);

        Assert.Equal(42, Assert.IsType<UserResponse>(
            Assert.IsType<OkObjectResult>(ok.Result).Value).Id);
        AssertMvcProblem(failed.Result!, StatusCodes.Status404NotFound, "user.missing");
        var createdResult = Assert.IsType<CreatedResult>(created.Result);
        Assert.Equal("/users/42", createdResult.Location);
        Assert.Equal(42, Assert.IsType<UserResponse>(createdResult.Value).Id);
        AssertMvcProblem(createFailed.Result!, StatusCodes.Status404NotFound, "user.missing");
        Assert.Equal(2, projectionInvocations);
        Assert.Equal(1, locationInvocations);
        Assert.Equal(2, mapperInvocations);
    }

    [Fact]
    public void ProjectedTerminals_ValidateDelegatesAndProjectionResults()
    {
        var user = new User(42);
        var applicationResult = Result.Success<User, ApplicationError>(user);
        var stringResult = Result.Success(user);
        var genericResult = Result.Success<User, DomainError>(user);
        Func<User, UserResponse> projection = value => new UserResponse(value.Id);
        Func<User, string> locationSelector = value => $"/users/{value.Id}";
        Func<string, ProblemDetails> stringMapper = _ => new() { Status = 500 };
        Func<DomainError, ProblemDetails> genericMapper = _ => new() { Status = 500 };

        Assert.Throws<ArgumentNullException>(() =>
            applicationResult.ToOkOrProblem<User, ApplicationError, UserResponse>(null!));
        Assert.Throws<ArgumentNullException>(() =>
            applicationResult.ToCreatedOrProblem(projection, null!));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToOkOrProblem<User, UserResponse>(null!, stringMapper));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToCreatedOrProblem(projection, locationSelector, null!));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToOkOrProblem<User, DomainError, UserResponse>(null!, genericMapper));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToCreatedOrProblem(projection, null!, genericMapper));
        Assert.Throws<ArgumentNullException>(() =>
            applicationResult.ToOkOrProblem(_ => (UserResponse)null!));
        Assert.Throws<ArgumentNullException>(() =>
            applicationResult.ToCreatedOrProblem(
                _ => (UserResponse)null!,
                locationSelector));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToOkOrProblem(_ => (UserResponse)null!, stringMapper));
        Assert.Throws<ArgumentNullException>(() =>
            stringResult.ToCreatedOrProblem(
                _ => (UserResponse)null!,
                locationSelector,
                stringMapper));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToOkOrProblem(_ => (UserResponse)null!, genericMapper));
        Assert.Throws<ArgumentNullException>(() =>
            genericResult.ToCreatedOrProblem(
                _ => (UserResponse)null!,
                locationSelector,
                genericMapper));
        Assert.Throws<InvalidOperationException>(() =>
            applicationResult.ToCreatedOrProblem(projection, _ => " "));
        Assert.Throws<InvalidOperationException>(() =>
            stringResult.ToCreatedOrProblem(projection, _ => " ", stringMapper));
        Assert.Throws<InvalidOperationException>(() =>
            genericResult.ToCreatedOrProblem(projection, _ => " ", genericMapper));
    }

    [Fact]
    public void TypedApplicationErrors_ToActionResult_InfersProjectedOutputType()
    {
        Func<User, ActionResult<UserResponse>> successMapper = value =>
            new OkObjectResult(new UserResponse(value.Id));

        ActionResult<UserResponse> result = Result
            .Success<User, ApplicationError>(new User(42))
            .ToActionResult(successMapper);
        ActionResult<UserResponse> failed = Result
            .Failure<User, ApplicationError>(
                new ApplicationError(
                    ErrorDefinition.NotFound("user.not_found", "User not found.")))
            .ToActionResult(successMapper);

        Assert.Equal(42, Assert.IsType<UserResponse>(
            Assert.IsType<OkObjectResult>(result.Result).Value).Id);
        AssertMvcProblem(failed.Result!, StatusCodes.Status404NotFound, "User not found.");
    }

    [Fact]
    public async Task TypedApplicationErrorExecution_AppliesProblemDetailsServiceCustomization()
    {
        var error = new ApplicationError(
            ErrorDefinition.Forbidden("user.forbidden", "Access is forbidden."));
        var result = Result.Failure<User, ApplicationError>(error).ToOkOrProblem();

        var response = await ExecuteAsync(
            result.Result!,
            services => services.AddProblemDetails(
                options => options.CustomizeProblemDetails = context =>
                    context.ProblemDetails.Extensions["customized"] = true));

        using var document = JsonDocument.Parse(response.Body);
        Assert.Equal(StatusCodes.Status403Forbidden, response.StatusCode);
        Assert.True(document.RootElement.GetProperty("customized").GetBoolean());
        Assert.Equal("user.forbidden", document.RootElement.GetProperty("code").GetString());
        Assert.Equal("/api/tests", document.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            document.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public void Delegates_AreValidatedAndOnlySelectedDelegateRuns()
    {
        var user = new User(42);
        var mapperInvocations = 0;
        var selectorInvocations = 0;
        Func<DomainError, ProblemDetails> mapper = error =>
        {
            mapperInvocations++;
            return new ProblemDetails { Status = 409, Detail = error.Code };
        };
        Func<User, string> selector = value =>
        {
            selectorInvocations++;
            return $"/users/{value.Id}";
        };

        Result.Success<User, DomainError>(user).ToCreatedOrProblem(selector, mapper);
        Assert.Equal(1, selectorInvocations);
        Assert.Equal(0, mapperInvocations);

        Result.Failure<User, DomainError>(new("conflict")).ToCreatedOrProblem(selector, mapper);
        Assert.Equal(1, selectorInvocations);
        Assert.Equal(1, mapperInvocations);

        Assert.Throws<ArgumentNullException>(() =>
            Result.Success<User, DomainError>(user).ToOkOrProblem(null!));
        Assert.Throws<ArgumentNullException>(() =>
            Result.Success<User, DomainError>(user).ToCreatedOrProblem(null!, mapper));
        Assert.Throws<ArgumentNullException>(() =>
            Result.Success<User, DomainError>(user).ToCreatedOrProblem(selector, null!));
        Assert.Throws<InvalidOperationException>(() =>
            Result.Failure<User, DomainError>(new("missing")).ToOkOrProblem(_ => null!));
        Assert.Throws<InvalidOperationException>(() =>
            Result.Failure<User, DomainError>(new("missing"))
                .ToOkOrProblem(_ => new ProblemDetails()));
        Assert.Throws<InvalidOperationException>(() =>
            Result.Success<User, DomainError>(user)
                .ToCreatedOrProblem(_ => string.Empty, mapper));
    }

    [Fact]
    public void UninitializedResults_FailThroughSafeMatching()
    {
        Func<DomainError, ProblemDetails> mapper = _ =>
            new ProblemDetails { Status = StatusCodes.Status500InternalServerError };

        Assert.Throws<InvalidOperationException>(() =>
            default(Result).ToOkOrProblem(ToInternalServerProblem));
        Assert.Throws<InvalidOperationException>(() =>
            default(Result<User>).ToOkOrProblem(ToInternalServerProblem));
        Assert.Throws<InvalidOperationException>(() =>
            default(Result<User, DomainError>).ToOkOrProblem(mapper));
        Assert.Throws<InvalidOperationException>(() =>
            default(UnitResult<DomainError>).ToNoContentOrProblem(mapper));
    }

    [Fact]
    public async Task ExecutedMvcResults_WriteExpectedStatusContentTypeAndJsonBodies()
    {
        var ok = Option.Some(new User(42)).ToOkOrNotFound();
        var okResponse = await ExecuteAsync(ok.Result!);
        Assert.Equal(StatusCodes.Status200OK, okResponse.StatusCode);
        Assert.Equal("application/json; charset=utf-8", okResponse.ContentType);
        using (var document = JsonDocument.Parse(okResponse.Body))
        {
            Assert.Equal(42, document.RootElement.GetProperty("id").GetInt32());
        }

        var created = Result.Success(new User(43))
            .ToCreatedOrProblem(
                user => $"/users/{user.Id}",
                ToInternalServerProblem);
        var createdResponse = await ExecuteAsync(created.Result!);
        Assert.Equal(StatusCodes.Status201Created, createdResponse.StatusCode);
        Assert.Equal("/users/43", createdResponse.Location);
        using (var document = JsonDocument.Parse(createdResponse.Body))
        {
            Assert.Equal(43, document.RootElement.GetProperty("id").GetInt32());
        }

        var problem = Result.Failure<User>("missing").ToOkOrProblem(ToInternalServerProblem);
        var problemResponse = await ExecuteAsync(problem.Result!);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResponse.StatusCode);
        Assert.Equal("application/problem+json", problemResponse.ContentType);
        using var problemDocument = JsonDocument.Parse(problemResponse.Body);
        Assert.Equal(
            "An unexpected error occurred.",
            problemDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(500, problemDocument.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            "/api/tests",
            problemDocument.RootElement.GetProperty("instance").GetString());
        Assert.False(string.IsNullOrWhiteSpace(
            problemDocument.RootElement.GetProperty("traceId").GetString()));
    }

    private static ProblemDetails ToInternalServerProblem(string _) =>
        new()
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = "An unexpected error occurred."
        };

    private static void AssertMvcProblem(ActionResult actual, int status, string? detail)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actual);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(status, objectResult.StatusCode);
        Assert.Equal(status, problem.Status);
        Assert.Equal(detail, problem.Detail);
        Assert.Contains("application/problem+json", objectResult.ContentTypes);
    }

    private static void AssertSameProblem(ProblemDetails expected, ActionResult? actual)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(actual);
        Assert.Same(expected, objectResult.Value);
        Assert.Equal(expected.Status, objectResult.StatusCode);
        Assert.Contains("application/problem+json", objectResult.ContentTypes);
    }

    private static void AssertLocationlessCreated<T>(ActionResult<T> actual, T expected)
    {
        var created = Assert.IsType<CreatedResult>(actual.Result);
        Assert.Equal(StatusCodes.Status201Created, created.StatusCode);
        Assert.Null(created.Location);
        Assert.Equal(expected, created.Value);
    }

    private static async Task<ResponseSnapshot> ExecuteAsync(
        ActionResult result,
        Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        configureServices?.Invoke(services);
        await using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };
        httpContext.Request.PathBase = "/api";
        httpContext.Request.Path = "/tests";
        await using var body = new MemoryStream();
        httpContext.Response.Body = body;
        var actionContext = new ActionContext(
            httpContext,
            new RouteData(),
            new ActionDescriptor());

        await result.ExecuteResultAsync(actionContext);
        body.Position = 0;
        using var reader = new StreamReader(body);
        return new ResponseSnapshot(
            httpContext.Response.StatusCode,
            httpContext.Response.ContentType,
            httpContext.Response.Headers.Location,
            await reader.ReadToEndAsync());
    }

    private sealed record User(int Id);

    private sealed record UserResponse(int Id);

    private sealed class TestController : ControllerBase
    {
        public ActionResult<User> ToUnauthorized(Option<User> option) =>
            option.ToOkOr(Unauthorized);

        public ActionResult<string> ToConflict(Option<User> option) =>
            option.ToOkOr(user => user.Id.ToString(), Conflict);
    }

    private sealed record DomainError(string Code);

    private sealed record ApplicationError(ErrorDefinition Definition) : IError;

    private sealed record ResponseSnapshot(
        int StatusCode,
        string? ContentType,
        string? Location,
        string Body);
}
