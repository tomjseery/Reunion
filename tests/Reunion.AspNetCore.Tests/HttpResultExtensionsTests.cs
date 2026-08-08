using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Reunion.AspNetCore.HttpResults;

namespace Reunion.AspNetCore.Tests;

public sealed class HttpResultExtensionsTests
{
    [Fact]
    public void OptionMappings_EveryCase_ReturnsExactTypedResult()
    {
        Results<Ok<User>, NotFound> found = Option.Some(new User(42)).ToOkOrNotFound();
        Results<Ok<User>, NotFound> missing = Option.None<User>().ToOkOrNotFound();
        Results<Ok<User>, NoContent> present = Option.Some(new User(43)).ToOkOrNoContent();
        Results<Ok<User>, NoContent> absent = Option.None<User>().ToOkOrNoContent();

        var foundResult = Assert.IsType<Ok<User>>(found.Result);
        Assert.Equal(StatusCodes.Status200OK, foundResult.StatusCode);
        Assert.Equal(42, foundResult.Value!.Id);
        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<NotFound>(missing.Result).StatusCode);
        Assert.Equal(43, Assert.IsType<Ok<User>>(present.Result).Value!.Id);
        Assert.Equal(StatusCodes.Status204NoContent, Assert.IsType<NoContent>(absent.Result).StatusCode);
    }

    [Fact]
    public void StringResultMappings_EveryCase_ReturnsExpectedStatusAndBody()
    {
        Results<Ok, ProblemHttpResult> ok = Result.Success().ToOkOrProblem();
        Results<Ok, ProblemHttpResult> failed = Result.Failure("operation failed").ToOkOrProblem();
        Results<NoContent, ProblemHttpResult> noContent = Result.Success().ToNoContentOrProblem();
        Results<NoContent, ProblemHttpResult> noContentFailed =
            Result.Failure("delete failed").ToNoContentOrProblem();

        Assert.Equal(StatusCodes.Status200OK, Assert.IsType<Ok>(ok.Result).StatusCode);
        AssertProblem(failed.Result, StatusCodes.Status500InternalServerError, "operation failed");
        Assert.Equal(
            StatusCodes.Status204NoContent,
            Assert.IsType<NoContent>(noContent.Result).StatusCode);
        AssertProblem(noContentFailed.Result, StatusCodes.Status500InternalServerError, "delete failed");
    }

    [Fact]
    public void ValueResultMappings_EveryCase_ReturnsExpectedStatusBodyAndLocation()
    {
        var user = new User(42);
        Results<Ok<User>, ProblemHttpResult> ok = Result.Success(user).ToOkOrProblem();
        Results<Ok<User>, ProblemHttpResult> failed =
            Result.Failure<User>("missing").ToOkOrProblem();
        Results<Created<User>, ProblemHttpResult> created =
            Result.Success(user).ToCreatedOrProblem(value => $"/users/{value.Id}");
        Results<Created<User>, ProblemHttpResult> createFailed =
            Result.Failure<User>("invalid").ToCreatedOrProblem(value => $"/users/{value.Id}");

        var okResult = Assert.IsType<Ok<User>>(ok.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Same(user, okResult.Value);
        AssertProblem(failed.Result, StatusCodes.Status500InternalServerError, "missing");

        var createdResult = Assert.IsType<Created<User>>(created.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal("/users/42", createdResult.Location);
        Assert.Same(user, createdResult.Value);
        AssertProblem(createFailed.Result, StatusCodes.Status500InternalServerError, "invalid");
    }

    [Fact]
    public void GenericResultMappings_EveryCase_PreservesMappedProblem()
    {
        var user = new User(42);
        var error = new DomainError("user.missing");
        var mappedProblem = TypedResults.Problem(new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "User not found",
            Detail = "The requested user does not exist.",
            Extensions = { ["code"] = error.Code }
        });
        Func<DomainError, ProblemHttpResult> mapper = _ => mappedProblem;

        Results<Ok<User>, ProblemHttpResult> ok =
            Result.Success<User, DomainError>(user).ToOkOrProblem(mapper);
        Results<Ok<User>, ProblemHttpResult> failed =
            Result.Failure<User, DomainError>(error).ToOkOrProblem(mapper);
        Results<Created<User>, ProblemHttpResult> created =
            Result.Success<User, DomainError>(user)
                .ToCreatedOrProblem(value => $"/users/{value.Id}", mapper);
        Results<Created<User>, ProblemHttpResult> createFailed =
            Result.Failure<User, DomainError>(error)
                .ToCreatedOrProblem(value => $"/users/{value.Id}", mapper);

        Assert.Same(user, Assert.IsType<Ok<User>>(ok.Result).Value);
        Assert.Same(mappedProblem, failed.Result);
        Assert.Equal("user.missing", mappedProblem.ProblemDetails.Extensions["code"]);
        Assert.Equal("/users/42", Assert.IsType<Created<User>>(created.Result).Location);
        Assert.Same(mappedProblem, createFailed.Result);
    }

    [Fact]
    public void UnitResultMappings_EveryCase_ReturnsExpectedTypedResult()
    {
        var error = new DomainError("delete.denied");
        var problem = TypedResults.Problem(
            detail: "Delete denied.",
            statusCode: StatusCodes.Status403Forbidden);
        Func<DomainError, ProblemHttpResult> mapper = _ => problem;

        Results<Ok, ProblemHttpResult> ok = UnitResult.Success<DomainError>().ToOkOrProblem(mapper);
        Results<Ok, ProblemHttpResult> failed = UnitResult.Failure(error).ToOkOrProblem(mapper);
        Results<NoContent, ProblemHttpResult> noContent =
            UnitResult.Success<DomainError>().ToNoContentOrProblem(mapper);
        Results<NoContent, ProblemHttpResult> noContentFailed =
            UnitResult.Failure(error).ToNoContentOrProblem(mapper);

        Assert.IsType<Ok>(ok.Result);
        Assert.Same(problem, failed.Result);
        Assert.IsType<NoContent>(noContent.Result);
        Assert.Same(problem, noContentFailed.Result);
    }

    [Fact]
    public void CustomStringErrorMapper_PreservesCallerProblem()
    {
        var problem = TypedResults.Problem(new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                ["name"] = ["Name is required."]
            })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Extensions = { ["code"] = "validation" }
        });

        var result = Result.Failure<User>("invalid").ToOkOrProblem(_ => problem);

        var actual = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Same(problem, actual);
        var details = Assert.IsType<ValidationProblemDetails>(actual.ProblemDetails);
        Assert.Equal("Name is required.", Assert.Single(details.Errors["name"]));
        Assert.Equal("validation", details.Extensions["code"]);
    }

    [Fact]
    public void Delegates_AreValidatedAndOnlySelectedDelegateRuns()
    {
        var user = new User(42);
        var mapperInvocations = 0;
        var selectorInvocations = 0;
        Func<DomainError, ProblemHttpResult> mapper = error =>
        {
            mapperInvocations++;
            return TypedResults.Problem(detail: error.Code, statusCode: 409);
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
            Result.Failure<User, DomainError>(new("missing"))
                .ToOkOrProblem(_ => null!));
        Assert.Throws<InvalidOperationException>(() =>
            Result.Success<User, DomainError>(user)
                .ToCreatedOrProblem(_ => " ", mapper));
    }

    [Fact]
    public void UninitializedResults_FailThroughSafeMatching()
    {
        Func<DomainError, ProblemHttpResult> mapper = _ =>
            TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);

        Assert.Throws<InvalidOperationException>(() => default(Result).ToOkOrProblem());
        Assert.Throws<InvalidOperationException>(() => default(Result<User>).ToOkOrProblem());
        Assert.Throws<InvalidOperationException>(() =>
            default(Result<User, DomainError>).ToOkOrProblem(mapper));
        Assert.Throws<InvalidOperationException>(() =>
            default(UnitResult<DomainError>).ToNoContentOrProblem(mapper));
    }

    [Fact]
    public async Task ExecutedHttpResults_WriteExpectedStatusContentTypeAndJsonBodies()
    {
        var ok = Option.Some(new User(42)).ToOkOrNotFound();
        var okResponse = await ExecuteAsync(ok);
        Assert.Equal(StatusCodes.Status200OK, okResponse.StatusCode);
        Assert.Equal("application/json; charset=utf-8", okResponse.ContentType);
        using (var document = JsonDocument.Parse(okResponse.Body))
        {
            Assert.Equal(42, document.RootElement.GetProperty("id").GetInt32());
        }

        var created = Result.Success(new User(43))
            .ToCreatedOrProblem(user => $"/users/{user.Id}");
        var createdResponse = await ExecuteAsync(created);
        Assert.Equal(StatusCodes.Status201Created, createdResponse.StatusCode);
        Assert.Equal("/users/43", createdResponse.Location);
        using (var document = JsonDocument.Parse(createdResponse.Body))
        {
            Assert.Equal(43, document.RootElement.GetProperty("id").GetInt32());
        }

        var problem = Result.Failure<User>("missing").ToOkOrProblem();
        var problemResponse = await ExecuteAsync(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResponse.StatusCode);
        Assert.Equal("application/problem+json", problemResponse.ContentType);
        using var problemDocument = JsonDocument.Parse(problemResponse.Body);
        Assert.Equal("missing", problemDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(500, problemDocument.RootElement.GetProperty("status").GetInt32());
    }

    private static void AssertProblem(IResult actual, int status, string detail)
    {
        var problem = Assert.IsType<ProblemHttpResult>(actual);
        Assert.Equal(status, problem.StatusCode);
        Assert.Equal(status, problem.ProblemDetails.Status);
        Assert.Equal(detail, problem.ProblemDetails.Detail);
    }

    private static async Task<ResponseSnapshot> ExecuteAsync(IResult result)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        services.ConfigureHttpJsonOptions(_ => { });
        await using var provider = services.BuildServiceProvider();
        var context = new DefaultHttpContext
        {
            RequestServices = provider
        };
        await using var body = new MemoryStream();
        context.Response.Body = body;

        await result.ExecuteAsync(context);
        body.Position = 0;
        using var reader = new StreamReader(body);
        return new ResponseSnapshot(
            context.Response.StatusCode,
            context.Response.ContentType,
            context.Response.Headers.Location,
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
