using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore.Mvc;

/// <summary>Maps result values to ASP.NET Core MVC action results.</summary>
public static class ResultActionResultExtensions
{
    /// <summary>Maps success with a caller-supplied action result and a typed error to problem details.</summary>
    [OverloadResolutionPriority(1)]
    public static ActionResult<TValue> ToActionResult<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, ActionResult<TValue>> successMapper)
        where TValue : notnull
        where TError : IError
        => result.ToActionResult<TValue, TError, TValue>(successMapper);

    /// <summary>Maps success to a caller-supplied action result with a different response type and a typed error to problem details.</summary>
    public static ActionResult<TResponse> ToActionResult<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, ActionResult<TResponse>> successMapper)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(successMapper);
        return result.Match<ActionResult<TResponse>>(
            value => MapSuccess(value, successMapper),
            error => ProblemDetails.Create(error).ToObjectResult());
    }

    /// <summary>Maps success with a caller-supplied action result and a typed error to problem details.</summary>
    public static ActionResult ToActionResult<TError>(
        this UnitResult<TError> result,
        Func<ActionResult> successMapper)
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(successMapper);
        return result.Match(
            () => MapSuccess(successMapper),
            error => ProblemDetails.Create(error).ToObjectResult());
    }

    /// <summary>Maps a successful value to OK and a typed error to problem details.</summary>
    public static ActionResult<TValue> ToOkOrProblem<TValue, TError>(
        this Result<TValue, TError> result)
        where TValue : notnull
        where TError : IError =>
        result.ToActionResult(value => new OkObjectResult(value));

    /// <summary>Projects a successful value to OK and maps a typed error to problem details.</summary>
    public static ActionResult<TResponse> ToOkOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        return result.Map(projection).ToOkOrProblem();
    }

    /// <summary>Maps a successful value to Created and a typed error to problem details.</summary>
    public static ActionResult<TValue> ToCreatedOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, string> locationSelector)
        where TValue : notnull
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        return result.ToActionResult(value => Create(value, locationSelector));
    }

    /// <summary>Projects a successful value to Created, selects its location from the original value, and maps a typed error to problem details.</summary>
    public static ActionResult<TResponse> ToCreatedOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        Func<TValue, string> locationSelector)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(locationSelector);
        return result.ToActionResult<TValue, TError, TResponse>(
            value => Create(value, projection, locationSelector));
    }

    /// <summary>Maps success to OK and a typed error to problem details.</summary>
    public static ActionResult ToOkOrProblem<TError>(this UnitResult<TError> result)
        where TError : IError =>
        result.ToActionResult(() => new OkResult());

    /// <summary>Maps success to No Content and a typed error to problem details.</summary>
    public static ActionResult ToNoContentOrProblem<TError>(this UnitResult<TError> result)
        where TError : IError =>
        result.ToActionResult(() => new NoContentResult());

    /// <summary>Maps success to OK and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult ToOkOrProblem(
        this Result result,
        Func<string, ProblemDetails> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult>(
            () => new OkResult(),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Maps success to No Content and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult ToNoContentOrProblem(
        this Result result,
        Func<string, ProblemDetails> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult>(
            () => new NoContentResult(),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Maps a successful value to OK and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TValue> ToOkOrProblem<TValue>(
        this Result<TValue> result,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult<TValue>>(
            value => new OkObjectResult(value),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Projects a successful value to OK and maps failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TResponse> ToOkOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Map(projection).ToOkOrProblem(errorMapper);
    }

    /// <summary>Maps a successful value to Created and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TValue> ToCreatedOrProblem<TValue>(
        this Result<TValue> result,
        Func<TValue, string> locationSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult<TValue>>(
            value => Create(value, locationSelector),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Projects a successful value to Created, selects its location from the original value, and maps failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TResponse> ToCreatedOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        Func<TValue, string> locationSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult<TResponse>>(
            value => Create(value, projection, locationSelector),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Maps a successful value to OK and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TValue> ToOkOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult<TValue>>(
            value => new OkObjectResult(value),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Projects a successful value to OK and maps failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TResponse> ToOkOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Map(projection).ToOkOrProblem(errorMapper);
    }

    /// <summary>Maps a successful value to Created and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TValue> ToCreatedOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, string> locationSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult<TValue>>(
            value => Create(value, locationSelector),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Projects a successful value to Created, selects its location from the original value, and maps failure with a caller-supplied problem mapper.</summary>
    public static ActionResult<TResponse> ToCreatedOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        Func<TValue, string> locationSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult<TResponse>>(
            value => Create(value, projection, locationSelector),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Maps success to OK and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult ToOkOrProblem<TError>(
        this UnitResult<TError> result,
        Func<TError, ProblemDetails> errorMapper)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult>(
            () => new OkResult(),
            error => errorMapper(error).ToObjectResult());
    }

    /// <summary>Maps success to No Content and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult ToNoContentOrProblem<TError>(
        this UnitResult<TError> result,
        Func<TError, ProblemDetails> errorMapper)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult>(
            () => new NoContentResult(),
            error => errorMapper(error).ToObjectResult());
    }

    private static ActionResult<TResponse> MapSuccess<TValue, TResponse>(
        TValue value,
        Func<TValue, ActionResult<TResponse>> successMapper)
        where TResponse : notnull =>
        successMapper(value)
            ?? throw new InvalidOperationException("The success mapper returned null.");

    private static TResponse Project<TValue, TResponse>(
        TValue value,
        Func<TValue, TResponse> projection)
        where TResponse : notnull
    {
        var response = projection(value);
        ArgumentNullException.ThrowIfNull(response);
        return response;
    }

    private static ActionResult MapSuccess(Func<ActionResult> successMapper) =>
        successMapper()
            ?? throw new InvalidOperationException("The success mapper returned null.");

    private static CreatedResult Create<TValue>(
        TValue value,
        Func<TValue, string> locationSelector)
        where TValue : notnull
    {
        var location = locationSelector(value);
        if (string.IsNullOrWhiteSpace(location))
            throw new InvalidOperationException("The location selector returned a null or whitespace location.");

        return new CreatedResult(location, value);
    }

    private static CreatedResult Create<TValue, TResponse>(
        TValue value,
        Func<TValue, TResponse> projection,
        Func<TValue, string> locationSelector)
        where TResponse : notnull
    {
        var response = Project(value, projection);
        var location = locationSelector(value);
        if (string.IsNullOrWhiteSpace(location))
            throw new InvalidOperationException("The location selector returned a null or whitespace location.");

        return new CreatedResult(location, response);
    }
}
