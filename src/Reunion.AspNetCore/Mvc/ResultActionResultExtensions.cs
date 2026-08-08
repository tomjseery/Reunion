using Microsoft.AspNetCore.Mvc;

namespace Reunion.AspNetCore.Mvc;

/// <summary>Maps result values to ASP.NET Core MVC action results.</summary>
public static class ResultActionResultExtensions
{
    /// <summary>Maps success to OK and a string failure to an Internal Server Error problem.</summary>
    public static ActionResult ToOkOrProblem(this Result result) =>
        result.Match<ActionResult>(
            () => new OkResult(),
            MvcProblemResults.FromString);

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

    /// <summary>Maps success to No Content and a string failure to an Internal Server Error problem.</summary>
    public static ActionResult ToNoContentOrProblem(this Result result) =>
        result.Match<ActionResult>(
            () => new NoContentResult(),
            MvcProblemResults.FromString);

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

    /// <summary>Maps a successful value to OK and a string failure to an Internal Server Error problem.</summary>
    public static ActionResult<TValue> ToOkOrProblem<TValue>(this Result<TValue> result)
        where TValue : notnull =>
        result.Match<ActionResult<TValue>>(
            value => new OkObjectResult(value),
            error => MvcProblemResults.FromString(error));

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

    /// <summary>Maps a successful value to Created and a string failure to an Internal Server Error problem.</summary>
    public static ActionResult<TValue> ToCreatedOrProblem<TValue>(
        this Result<TValue> result,
        Func<TValue, string> locationSelector)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        return result.Match<ActionResult<TValue>>(
            value => Create(value, locationSelector),
            error => MvcProblemResults.FromString(error));
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
