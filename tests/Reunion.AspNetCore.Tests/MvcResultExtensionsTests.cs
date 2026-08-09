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

        var foundResult = Assert.IsType<OkObjectResult>(found.Result);
        Assert.Equal(StatusCodes.Status200OK, foundResult.StatusCode);
        Assert.Equal(42, Assert.IsType<User>(foundResult.Value).Id);
        Assert.IsType<NotFoundResult>(missing.Result);
        Assert.Equal(43, Assert.IsType<User>(Assert.IsType<OkObjectResult>(present.Result).Value).Id);
        Assert.IsType<NoContentResult>(absent.Result);
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
                .ToActionResult(_ => null!));
        Assert.Throws<InvalidOperationException>(() =>
            UnitResult.Success<ApplicationError>()
                .ToActionResult(() => null!));
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

    private static void AssertMvcProblem(ActionResult actual, int status, string detail)
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

    private sealed record DomainError(string Code);

    private sealed record ApplicationError(ErrorDefinition Definition) : IError;

    private sealed record ResponseSnapshot(
        int StatusCode,
        string? ContentType,
        string? Location,
        string Body);
}
