using Microsoft.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore.Mvc;

/// <summary>Maps result values to ASP.NET Core MVC action results.</summary>
public static class ResultActionResultExtensions
{
    /// <summary>Maps success with a caller-supplied action result and a typed error to problem details.</summary>
    public static ActionResult<TValue> ToActionResult<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, ActionResult<TValue>> successMapper)
        where TValue : notnull
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(successMapper);
        return result.Match(
            value => MapSuccess(value, successMapper),
            error => MvcProblemResults.FromProblemDetails(ErrorProblemDetails.Create(error)));
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
            error => MvcProblemResults.FromProblemDetails(ErrorProblemDetails.Create(error)));
    }

    /// <summary>Maps a successful value to OK and a typed error to problem details.</summary>
    public static ActionResult<TValue> ToOkOrProblem<TValue, TError>(
        this Result<TValue, TError> result)
        where TValue : notnull
        where TError : IError =>
        result.ToActionResult(value => new OkObjectResult(value));

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
            error => MvcProblemResults.Map(error, errorMapper));
    }

    /// <summary>Maps success to No Content and failure with a caller-supplied problem mapper.</summary>
    public static ActionResult ToNoContentOrProblem(
        this Result result,
        Func<string, ProblemDetails> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<ActionResult>(
            () => new NoContentResult(),
            error => MvcProblemResults.Map(error, errorMapper));
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
            error => MvcProblemResults.Map(error, errorMapper));
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
            error => MvcProblemResults.Map(error, errorMapper));
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
            error => MvcProblemResults.Map(error, errorMapper));
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
            error => MvcProblemResults.Map(error, errorMapper));
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
            error => MvcProblemResults.Map(error, errorMapper));
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
            error => MvcProblemResults.Map(error, errorMapper));
    }

    private static ActionResult<TValue> MapSuccess<TValue>(
        TValue value,
        Func<TValue, ActionResult<TValue>> successMapper)
        where TValue : notnull =>
        successMapper(value)
            ?? throw new InvalidOperationException("The success mapper returned null.");

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
}
