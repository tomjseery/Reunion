using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Reunion.AspNetCore.Mvc;

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
    public void StringResultMappings_EveryCase_ReturnsExpectedMvcResult()
    {
        ActionResult ok = Result.Success().ToOkOrProblem();
        ActionResult failed = Result.Failure("operation failed").ToOkOrProblem();
        ActionResult noContent = Result.Success().ToNoContentOrProblem();
        ActionResult noContentFailed = Result.Failure("delete failed").ToNoContentOrProblem();

        Assert.IsType<OkResult>(ok);
        AssertMvcProblem(failed, StatusCodes.Status500InternalServerError, "operation failed");
        Assert.IsType<NoContentResult>(noContent);
        AssertMvcProblem(noContentFailed, StatusCodes.Status500InternalServerError, "delete failed");
    }

    [Fact]
    public void ValueResultMappings_EveryCase_ReturnsExpectedMvcResult()
    {
        var user = new User(42);
        ActionResult<User> ok = Result.Success(user).ToOkOrProblem();
        ActionResult<User> failed = Result.Failure<User>("missing").ToOkOrProblem();
        ActionResult<User> created =
            Result.Success(user).ToCreatedOrProblem(value => $"/users/{value.Id}");
        ActionResult<User> createFailed =
            Result.Failure<User>("invalid").ToCreatedOrProblem(value => $"/users/{value.Id}");

        Assert.Same(user, Assert.IsType<OkObjectResult>(ok.Result).Value);
        AssertMvcProblem(failed.Result!, StatusCodes.Status500InternalServerError, "missing");

        var createdResult = Assert.IsType<CreatedResult>(created.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal("/users/42", createdResult.Location);
        Assert.Same(user, createdResult.Value);
        AssertMvcProblem(createFailed.Result!, StatusCodes.Status500InternalServerError, "invalid");
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

        Assert.Throws<InvalidOperationException>(() => default(Result).ToOkOrProblem());
        Assert.Throws<InvalidOperationException>(() => default(Result<User>).ToOkOrProblem());
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
            .ToCreatedOrProblem(user => $"/users/{user.Id}");
        var createdResponse = await ExecuteAsync(created.Result!);
        Assert.Equal(StatusCodes.Status201Created, createdResponse.StatusCode);
        Assert.Equal("/users/43", createdResponse.Location);
        using (var document = JsonDocument.Parse(createdResponse.Body))
        {
            Assert.Equal(43, document.RootElement.GetProperty("id").GetInt32());
        }

        var problem = Result.Failure<User>("missing").ToOkOrProblem();
        var problemResponse = await ExecuteAsync(problem.Result!);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResponse.StatusCode);
        Assert.Equal("application/problem+json; charset=utf-8", problemResponse.ContentType);
        using var problemDocument = JsonDocument.Parse(problemResponse.Body);
        Assert.Equal("missing", problemDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(500, problemDocument.RootElement.GetProperty("status").GetInt32());
    }

    private static void AssertMvcProblem(ActionResult actual, int status, string detail)
    {
        var objectResult = Assert.IsType<ObjectResult>(actual);
        var problem = Assert.IsType<ProblemDetails>(objectResult.Value);
        Assert.Equal(status, objectResult.StatusCode);
        Assert.Equal(status, problem.Status);
        Assert.Equal(detail, problem.Detail);
        Assert.Contains("application/problem+json", objectResult.ContentTypes);
    }

    private static void AssertSameProblem(ProblemDetails expected, ActionResult? actual)
    {
        var objectResult = Assert.IsType<ObjectResult>(actual);
        Assert.Same(expected, objectResult.Value);
        Assert.Equal(expected.Status, objectResult.StatusCode);
        Assert.Contains("application/problem+json", objectResult.ContentTypes);
    }

    private static async Task<ResponseSnapshot> ExecuteAsync(ActionResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers();
        await using var provider = services.BuildServiceProvider();
        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider
        };
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

    private sealed record ResponseSnapshot(
        int StatusCode,
        string? ContentType,
        string? Location,
        string Body);
}
