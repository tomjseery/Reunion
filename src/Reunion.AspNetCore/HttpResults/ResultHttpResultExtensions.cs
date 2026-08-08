using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Reunion.AspNetCore.HttpResults;

/// <summary>Maps result values to strongly typed ASP.NET Core HTTP results.</summary>
public static class ResultHttpResultExtensions
{
    /// <summary>Maps success to OK and a string failure to an Internal Server Error problem.</summary>
    public static Results<Ok, ProblemHttpResult> ToOkOrProblem(this Result result) =>
        result.Match<Results<Ok, ProblemHttpResult>>(
            () => TypedResults.Ok(),
            error => CreateStringProblem(error));

    /// <summary>Maps success to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok, ProblemHttpResult> ToOkOrProblem(
        this Result result,
        Func<string, ProblemHttpResult> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Ok, ProblemHttpResult>>(
            () => TypedResults.Ok(),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps success to No Content and a string failure to an Internal Server Error problem.</summary>
    public static Results<NoContent, ProblemHttpResult> ToNoContentOrProblem(
        this Result result) =>
        result.Match<Results<NoContent, ProblemHttpResult>>(
            () => TypedResults.NoContent(),
            error => CreateStringProblem(error));

    /// <summary>Maps success to No Content and failure with a caller-supplied problem mapper.</summary>
    public static Results<NoContent, ProblemHttpResult> ToNoContentOrProblem(
        this Result result,
        Func<string, ProblemHttpResult> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<NoContent, ProblemHttpResult>>(
            () => TypedResults.NoContent(),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps a successful value to OK and a string failure to an Internal Server Error problem.</summary>
    public static Results<Ok<TValue>, ProblemHttpResult> ToOkOrProblem<TValue>(
        this Result<TValue> result)
        where TValue : notnull =>
        result.Match<Results<Ok<TValue>, ProblemHttpResult>>(
            value => TypedResults.Ok(value),
            error => CreateStringProblem(error));

    /// <summary>Maps a successful value to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok<TValue>, ProblemHttpResult> ToOkOrProblem<TValue>(
        this Result<TValue> result,
        Func<string, ProblemHttpResult> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Ok<TValue>, ProblemHttpResult>>(
            value => TypedResults.Ok(value),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps a successful value to Created and a string failure to an Internal Server Error problem.</summary>
    public static Results<Created<TValue>, ProblemHttpResult> ToCreatedOrProblem<TValue>(
        this Result<TValue> result,
        Func<TValue, string> locationSelector)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        return result.Match<Results<Created<TValue>, ProblemHttpResult>>(
            value => Create(value, locationSelector),
            error => CreateStringProblem(error));
    }

    /// <summary>Maps a successful value to Created and failure with a caller-supplied problem mapper.</summary>
    public static Results<Created<TValue>, ProblemHttpResult> ToCreatedOrProblem<TValue>(
        this Result<TValue> result,
        Func<TValue, string> locationSelector,
        Func<string, ProblemHttpResult> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Created<TValue>, ProblemHttpResult>>(
            value => Create(value, locationSelector),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps a successful value to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok<TValue>, ProblemHttpResult> ToOkOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, ProblemHttpResult> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Ok<TValue>, ProblemHttpResult>>(
            value => TypedResults.Ok(value),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps a successful value to Created and failure with a caller-supplied problem mapper.</summary>
    public static Results<Created<TValue>, ProblemHttpResult> ToCreatedOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, string> locationSelector,
        Func<TError, ProblemHttpResult> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Created<TValue>, ProblemHttpResult>>(
            value => Create(value, locationSelector),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps success to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok, ProblemHttpResult> ToOkOrProblem<TError>(
        this UnitResult<TError> result,
        Func<TError, ProblemHttpResult> errorMapper)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Ok, ProblemHttpResult>>(
            () => TypedResults.Ok(),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps success to No Content and failure with a caller-supplied problem mapper.</summary>
    public static Results<NoContent, ProblemHttpResult> ToNoContentOrProblem<TError>(
        this UnitResult<TError> result,
        Func<TError, ProblemHttpResult> errorMapper)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<NoContent, ProblemHttpResult>>(
            () => TypedResults.NoContent(),
            error => MapProblem(error, errorMapper));
    }

    private static ProblemHttpResult CreateStringProblem(string error) =>
        TypedResults.Problem(StringProblemDetails.Create(error));

    private static ProblemHttpResult MapProblem<TError>(
        TError error,
        Func<TError, ProblemHttpResult> errorMapper) =>
        errorMapper(error)
        ?? throw new InvalidOperationException("The error mapper returned null.");

    private static Created<TValue> Create<TValue>(
        TValue value,
        Func<TValue, string> locationSelector)
        where TValue : notnull
    {
        var location = locationSelector(value);
        if (string.IsNullOrWhiteSpace(location))
            throw new InvalidOperationException("The location selector returned a null or whitespace location.");

        return TypedResults.Created(location, value);
    }
}
