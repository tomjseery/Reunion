using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore.HttpResults;

/// <summary>Maps result values to strongly typed ASP.NET Core HTTP results.</summary>
public static class ResultHttpResultExtensions
{
    /// <summary>Maps success with a caller-supplied HTTP result and a typed error to problem details.</summary>
    public static Results<TSuccess, ProblemHttpResult> ToResults<TValue, TError, TSuccess>(
        this Result<TValue, TError> result,
        Func<TValue, TSuccess> successMapper)
        where TValue : notnull
        where TError : IError
        where TSuccess : IResult
    {
        ArgumentNullException.ThrowIfNull(successMapper);
        return result.Match<Results<TSuccess, ProblemHttpResult>>(
            value => MapSuccess(value, successMapper),
            error => TypedResults.Problem(ProblemDetails.Create(error)));
    }

    /// <summary>Maps success with a caller-supplied HTTP result and a typed error to problem details.</summary>
    public static Results<TSuccess, ProblemHttpResult> ToResults<TError, TSuccess>(
        this UnitResult<TError> result,
        Func<TSuccess> successMapper)
        where TError : IError
        where TSuccess : IResult
    {
        ArgumentNullException.ThrowIfNull(successMapper);
        return result.Match<Results<TSuccess, ProblemHttpResult>>(
            () => MapSuccess(successMapper),
            error => TypedResults.Problem(ProblemDetails.Create(error)));
    }

    /// <summary>Maps a successful value to OK and a typed error to problem details.</summary>
    public static Results<Ok<TValue>, ProblemHttpResult> ToOkOrProblem<TValue, TError>(
        this Result<TValue, TError> result)
        where TValue : notnull
        where TError : IError =>
        result.ToResults(value => TypedResults.Ok(value));

    /// <summary>Projects a successful value to OK and maps a typed error to problem details.</summary>
    public static Results<Ok<TResponse>, ProblemHttpResult> ToOkOrProblem<TValue, TError, TResponse>(
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
    public static Results<Created<TValue>, ProblemHttpResult> ToCreatedOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, string> locationSelector)
        where TValue : notnull
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        return result.ToResults(value => Create(value, locationSelector));
    }

    /// <summary>Projects a successful value to Created, selects its location from the original value, and maps a typed error to problem details.</summary>
    public static Results<Created<TResponse>, ProblemHttpResult> ToCreatedOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        Func<TValue, string> locationSelector)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(locationSelector);
        return result.ToResults(value => Create(value, projection, locationSelector));
    }

    /// <summary>Maps success to OK and a typed error to problem details.</summary>
    public static Results<Ok, ProblemHttpResult> ToOkOrProblem<TError>(
        this UnitResult<TError> result)
        where TError : IError =>
        result.ToResults(TypedResults.Ok);

    /// <summary>Maps success to No Content and a typed error to problem details.</summary>
    public static Results<NoContent, ProblemHttpResult> ToNoContentOrProblem<TError>(
        this UnitResult<TError> result)
        where TError : IError =>
        result.ToResults(TypedResults.NoContent);

    /// <summary>Maps success to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok, ProblemHttpResult> ToOkOrProblem(
        this Result result,
        Func<string, ProblemDetails> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Ok, ProblemHttpResult>>(
            () => TypedResults.Ok(),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps success to No Content and failure with a caller-supplied problem mapper.</summary>
    public static Results<NoContent, ProblemHttpResult> ToNoContentOrProblem(
        this Result result,
        Func<string, ProblemDetails> errorMapper)
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<NoContent, ProblemHttpResult>>(
            () => TypedResults.NoContent(),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps a successful value to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok<TValue>, ProblemHttpResult> ToOkOrProblem<TValue>(
        this Result<TValue> result,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Ok<TValue>, ProblemHttpResult>>(
            value => TypedResults.Ok(value),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Projects a successful value to OK and maps failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok<TResponse>, ProblemHttpResult> ToOkOrProblem<TValue, TResponse>(
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
    public static Results<Created<TValue>, ProblemHttpResult> ToCreatedOrProblem<TValue>(
        this Result<TValue> result,
        Func<TValue, string> locationSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Created<TValue>, ProblemHttpResult>>(
            value => Create(value, locationSelector),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Projects a successful value to Created, selects its location from the original value, and maps failure with a caller-supplied problem mapper.</summary>
    public static Results<Created<TResponse>, ProblemHttpResult> ToCreatedOrProblem<TValue, TResponse>(
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
        return result.Match<Results<Created<TResponse>, ProblemHttpResult>>(
            value => Create(value, projection, locationSelector),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps a successful value to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok<TValue>, ProblemHttpResult> ToOkOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Ok<TValue>, ProblemHttpResult>>(
            value => TypedResults.Ok(value),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Projects a successful value to OK and maps failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok<TResponse>, ProblemHttpResult> ToOkOrProblem<TValue, TError, TResponse>(
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
    public static Results<Created<TValue>, ProblemHttpResult> ToCreatedOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        Func<TValue, string> locationSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(locationSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<Created<TValue>, ProblemHttpResult>>(
            value => Create(value, locationSelector),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Projects a successful value to Created, selects its location from the original value, and maps failure with a caller-supplied problem mapper.</summary>
    public static Results<Created<TResponse>, ProblemHttpResult> ToCreatedOrProblem<TValue, TError, TResponse>(
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
        return result.Match<Results<Created<TResponse>, ProblemHttpResult>>(
            value => Create(value, projection, locationSelector),
            error => MapProblem(error, errorMapper));
    }

    /// <summary>Maps success to OK and failure with a caller-supplied problem mapper.</summary>
    public static Results<Ok, ProblemHttpResult> ToOkOrProblem<TError>(
        this UnitResult<TError> result,
        Func<TError, ProblemDetails> errorMapper)
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
        Func<TError, ProblemDetails> errorMapper)
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<NoContent, ProblemHttpResult>>(
            () => TypedResults.NoContent(),
            error => MapProblem(error, errorMapper));
    }

    private static ProblemHttpResult MapProblem<TError>(
        TError error,
        Func<TError, ProblemDetails> errorMapper)
    {
        var problemDetails = errorMapper(error)
            ?? throw new InvalidOperationException("The error mapper returned null.");

        if (problemDetails.Status is null)
            throw new InvalidOperationException("The mapped problem details must specify a status code.");

        return TypedResults.Problem(problemDetails);
    }

    private static TSuccess MapSuccess<TValue, TSuccess>(
        TValue value,
        Func<TValue, TSuccess> successMapper)
        where TSuccess : IResult =>
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

    private static TSuccess MapSuccess<TSuccess>(Func<TSuccess> successMapper)
        where TSuccess : IResult =>
        successMapper()
            ?? throw new InvalidOperationException("The success mapper returned null.");

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

    private static Created<TResponse> Create<TValue, TResponse>(
        TValue value,
        Func<TValue, TResponse> projection,
        Func<TValue, string> locationSelector)
        where TResponse : notnull
    {
        var response = Project(value, projection);
        var location = locationSelector(value);
        if (string.IsNullOrWhiteSpace(location))
            throw new InvalidOperationException("The location selector returned a null or whitespace location.");

        return TypedResults.Created(location, response);
    }
}
