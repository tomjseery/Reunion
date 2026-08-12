using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Reunion.AspNetCore.HttpResults;
using Reunion.Errors;

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
        Results<Ok<string>, NotFound> projectedFound = Option.Some(new User(44))
            .ToOkOrNotFound(user => user.Id.ToString());
        Results<Ok<string>, NoContent> projectedAbsent = Option.None<User>()
            .ToOkOrNoContent(user => user.Id.ToString());

        var foundResult = Assert.IsType<Ok<User>>(found.Result);
        Assert.Equal(StatusCodes.Status200OK, foundResult.StatusCode);
        Assert.Equal(42, foundResult.Value!.Id);
        Assert.Equal(StatusCodes.Status404NotFound, Assert.IsType<NotFound>(missing.Result).StatusCode);
        Assert.Equal(43, Assert.IsType<Ok<User>>(present.Result).Value!.Id);
        Assert.Equal(StatusCodes.Status204NoContent, Assert.IsType<NoContent>(absent.Result).StatusCode);
        Assert.Equal("44", Assert.IsType<Ok<string>>(projectedFound.Result).Value);
        Assert.IsType<NoContent>(projectedAbsent.Result);
    }

    [Fact]
    public void ToOkOr_MapsAbsenceWithCallerSelectedTypedResult()
    {
        Results<Ok<User>, UnauthorizedHttpResult> found = Option.Some(new User(42))
            .ToOkOr(TypedResults.Unauthorized);
        Results<Ok<User>, UnauthorizedHttpResult> missing = Option.None<User>()
            .ToOkOr(TypedResults.Unauthorized);
        Results<Ok<string>, UnauthorizedHttpResult> projected = Option.Some(new User(43))
            .ToOkOr(user => user.Id.ToString(), TypedResults.Unauthorized);

        Assert.Equal(42, Assert.IsType<Ok<User>>(found.Result).Value!.Id);
        Assert.IsType<UnauthorizedHttpResult>(missing.Result);
        Assert.Equal("43", Assert.IsType<Ok<string>>(projected.Result).Value);
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
        Func<UnauthorizedHttpResult> alternative = () =>
        {
            alternativeInvocations++;
            return TypedResults.Unauthorized();
        };

        Option.Some(new User(42)).ToOkOr(projection, alternative);
        Assert.Equal(1, projectionInvocations);
        Assert.Equal(0, alternativeInvocations);

        Option.None<User>().ToOkOr(projection, alternative);
        Assert.Equal(1, projectionInvocations);
        Assert.Equal(1, alternativeInvocations);

        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42)).ToOkOr<User, UnauthorizedHttpResult>(null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42))
                .ToOkOr<User, string, UnauthorizedHttpResult>(null!, alternative));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42))
                .ToOkOr<User, string, UnauthorizedHttpResult>(projection, null!));
        Assert.Throws<InvalidOperationException>(() =>
            Option.None<User>().ToOkOr(() => (UnauthorizedHttpResult)null!));
        Assert.Throws<ArgumentNullException>(() =>
            Option.Some(new User(42)).ToOkOr(_ => (string)null!, alternative));
    }

    [Fact]
    public void StringResultMappings_RequireMapperAndDoNotExposeError()
    {
        Results<Ok, ProblemHttpResult> ok = Result.Success().ToOkOrProblem(ToInternalServerProblem);
        Results<Ok, ProblemHttpResult> failed = Result.Failure("operation failed")
            .ToOkOrProblem(ToInternalServerProblem);
        Results<NoContent, ProblemHttpResult> noContent = Result.Success()
            .ToNoContentOrProblem(ToInternalServerProblem);
        Results<NoContent, ProblemHttpResult> noContentFailed =
            Result.Failure("delete failed").ToNoContentOrProblem(ToInternalServerProblem);

        Assert.Equal(StatusCodes.Status200OK, Assert.IsType<Ok>(ok.Result).StatusCode);
        AssertProblem(
            failed.Result,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.");
        Assert.Equal(
            StatusCodes.Status204NoContent,
            Assert.IsType<NoContent>(noContent.Result).StatusCode);
        AssertProblem(
            noContentFailed.Result,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.");
    }

    [Fact]
    public void ValueResultMappings_EveryCase_ReturnsExpectedStatusBodyAndLocation()
    {
        var user = new User(42);
        Results<Ok<User>, ProblemHttpResult> ok = Result.Success(user)
            .ToOkOrProblem(ToInternalServerProblem);
        Results<Ok<User>, ProblemHttpResult> failed =
            Result.Failure<User>("missing").ToOkOrProblem(ToInternalServerProblem);
        Results<Created<User>, ProblemHttpResult> created =
            Result.Success(user).ToCreatedOrProblem(
                value => $"/users/{value.Id}",
                ToInternalServerProblem);
        Results<Created<User>, ProblemHttpResult> createFailed =
            Result.Failure<User>("invalid").ToCreatedOrProblem(
                value => $"/users/{value.Id}",
                ToInternalServerProblem);

        var okResult = Assert.IsType<Ok<User>>(ok.Result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.Same(user, okResult.Value);
        AssertProblem(
            failed.Result,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.");

        var createdResult = Assert.IsType<Created<User>>(created.Result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        Assert.Equal("/users/42", createdResult.Location);
        Assert.Same(user, createdResult.Value);
        AssertProblem(
            createFailed.Result,
            StatusCodes.Status500InternalServerError,
            "An unexpected error occurred.");
    }

    [Fact]
    public void GenericResultMappings_EveryCase_PreservesMappedProblem()
    {
        var user = new User(42);
        var error = new DomainError("user.missing");
        var mappedDetails = new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "User not found",
            Detail = "The requested user does not exist.",
            Extensions = { ["code"] = error.Code }
        };
        Func<DomainError, ProblemDetails> mapper = _ => mappedDetails;

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
        var failedProblem = Assert.IsType<ProblemHttpResult>(failed.Result);
        Assert.Same(mappedDetails, failedProblem.ProblemDetails);
        Assert.Equal("user.missing", mappedDetails.Extensions["code"]);
        Assert.Equal("/users/42", Assert.IsType<Created<User>>(created.Result).Location);
        Assert.Same(
            mappedDetails,
            Assert.IsType<ProblemHttpResult>(createFailed.Result).ProblemDetails);
    }

    [Fact]
    public void UnitResultMappings_EveryCase_ReturnsExpectedTypedResult()
    {
        var error = new DomainError("delete.denied");
        var problem = new ProblemDetails
        {
            Detail = "Delete denied.",
            Status = StatusCodes.Status403Forbidden
        };
        Func<DomainError, ProblemDetails> mapper = _ => problem;

        Results<Ok, ProblemHttpResult> ok = UnitResult.Success<DomainError>().ToOkOrProblem(mapper);
        Results<Ok, ProblemHttpResult> failed = UnitResult.Failure(error).ToOkOrProblem(mapper);
        Results<NoContent, ProblemHttpResult> noContent =
            UnitResult.Success<DomainError>().ToNoContentOrProblem(mapper);
        Results<NoContent, ProblemHttpResult> noContentFailed =
            UnitResult.Failure(error).ToNoContentOrProblem(mapper);

        Assert.IsType<Ok>(ok.Result);
        Assert.Same(
            problem,
            Assert.IsType<ProblemHttpResult>(failed.Result).ProblemDetails);
        Assert.IsType<NoContent>(noContent.Result);
        Assert.Same(
            problem,
            Assert.IsType<ProblemHttpResult>(noContentFailed.Result).ProblemDetails);
    }

    [Fact]
    public void TypedApplicationErrors_MapWithoutCallerMapper()
    {
        var error = new ApplicationError(
            ErrorDefinition.NotFound("user.not_found", "User not found."));

        var result = Result.Failure<User, ApplicationError>(error).ToOkOrProblem();

        var problem = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Equal(StatusCodes.Status404NotFound, problem.StatusCode);
        Assert.Equal("User not found.", problem.ProblemDetails.Detail);
        Assert.Equal("user.not_found", problem.ProblemDetails.Extensions["code"]);
    }

    [Fact]
    public void TypedApplicationErrors_ToResults_DispatchesCustomSuccessAndFailure()
    {
        var user = new User(42);
        var error = new ApplicationError(
            ErrorDefinition.Conflict("user.conflict", "User conflict."));

        Results<Accepted<User>, ProblemHttpResult> accepted =
            Result.Success<User, ApplicationError>(user)
                .ToResults(value => TypedResults.Accepted($"/jobs/{value.Id}", value));
        Results<Accepted<User>, ProblemHttpResult> failed =
            Result.Failure<User, ApplicationError>(error)
                .ToResults(value => TypedResults.Accepted($"/jobs/{value.Id}", value));
        Results<Accepted, ProblemHttpResult> completed =
            UnitResult.Success<ApplicationError>().ToResults(() => TypedResults.Accepted("/jobs"));
        Results<Accepted, ProblemHttpResult> completionFailed =
            UnitResult.Failure(error).ToResults(() => TypedResults.Accepted("/jobs"));

        Assert.Equal("/jobs/42", Assert.IsType<Accepted<User>>(accepted.Result).Location);
        Assert.Equal(
            StatusCodes.Status409Conflict,
            Assert.IsType<ProblemHttpResult>(failed.Result).StatusCode);
        Assert.IsType<Accepted>(completed.Result);
        Assert.Equal(
            StatusCodes.Status409Conflict,
            Assert.IsType<ProblemHttpResult>(completionFailed.Result).StatusCode);
    }

    [Fact]
    public void TypedApplicationErrors_ToResults_RejectsNullSuccessResults()
    {
        Assert.Throws<InvalidOperationException>(() =>
            Result.Success<User, ApplicationError>(new User(42))
                .ToResults<User, ApplicationError, IResult>(_ => null!));
        Assert.Throws<InvalidOperationException>(() =>
            UnitResult.Success<ApplicationError>()
                .ToResults<ApplicationError, IResult>(() => null!));
    }

    [Fact]
    public void CustomStringErrorMapper_PreservesCallerProblem()
    {
        var problem = new ValidationProblemDetails(
            new Dictionary<string, string[]>
            {
                ["name"] = ["Name is required."]
            })
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Extensions = { ["code"] = "validation" }
        };

        var result = Result.Failure<User>("invalid").ToOkOrProblem(_ => problem);

        var actual = Assert.IsType<ProblemHttpResult>(result.Result);
        Assert.Same(problem, actual.ProblemDetails);
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
        Func<DomainError, ProblemDetails> mapper = error =>
        {
            mapperInvocations++;
            return new ProblemDetails { Detail = error.Code, Status = 409 };
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
        Func<DomainError, ProblemDetails> mapper = _ =>
            new() { Status = StatusCodes.Status500InternalServerError };

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
            .ToCreatedOrProblem(
                user => $"/users/{user.Id}",
                ToInternalServerProblem);
        var createdResponse = await ExecuteAsync(created);
        Assert.Equal(StatusCodes.Status201Created, createdResponse.StatusCode);
        Assert.Equal("/users/43", createdResponse.Location);
        using (var document = JsonDocument.Parse(createdResponse.Body))
        {
            Assert.Equal(43, document.RootElement.GetProperty("id").GetInt32());
        }

        var problem = Result.Failure<User>("missing").ToOkOrProblem(ToInternalServerProblem);
        var problemResponse = await ExecuteAsync(problem);
        Assert.Equal(StatusCodes.Status500InternalServerError, problemResponse.StatusCode);
        Assert.Equal("application/problem+json", problemResponse.ContentType);
        using var problemDocument = JsonDocument.Parse(problemResponse.Body);
        Assert.Equal(
            "An unexpected error occurred.",
            problemDocument.RootElement.GetProperty("detail").GetString());
        Assert.Equal(500, problemDocument.RootElement.GetProperty("status").GetInt32());
    }

    private static ProblemDetails ToInternalServerProblem(string _) =>
        new()
        {
            Detail = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error"
        };

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

    private sealed record ApplicationError(ErrorDefinition Definition) : IError;

    private sealed record ResponseSnapshot(
        int StatusCode,
        string? ContentType,
        string? Location,
        string Body);
}
