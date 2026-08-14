using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore.HttpResults;

public static partial class ResultHttpResultExtensions
{
    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result)
        where TValue : notnull
        where TError : IError =>
        result.ToCreatedAtRouteOrProblem(routeName: null, routeValues: null);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName)
        where TValue : notnull
        where TError : IError =>
        result.ToCreatedAtRouteOrProblem(routeName, routeValues: null);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        object? routeValues)
        where TValue : notnull
        where TError : IError =>
        result.ToResults(value => TypedResults.CreatedAtRoute(value, routeName, routeValues));

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector)
        where TValue : notnull
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        return result.ToResults(
            value => TypedResults.CreatedAtRoute(value, routeName, routeValuesSelector(value)));
    }

    /// <summary>Projects success to CreatedAtRoute and maps failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull =>
        result.ToCreatedAtRouteOrProblem(projection, routeName: null, routeValues: null);

    /// <summary>Projects success to CreatedAtRoute and maps failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull =>
        result.ToCreatedAtRouteOrProblem(projection, routeName, routeValues: null);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        return result.ToResults(value =>
            TypedResults.CreatedAtRoute(Project(value, projection), routeName, routeValues));
    }

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        return result.ToResults(value =>
            TypedResults.CreatedAtRoute(
                Project(value, projection),
                routeName,
                routeValuesSelector(value)));
    }

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue>(
        this Result<TValue> result,
        string? routeName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull =>
        MapCreatedAtRoute(result, routeName, routeValues, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue>(
        this Result<TValue> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull =>
        MapCreatedAtRoute(result, routeName, routeValuesSelector, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull =>
        MapCreatedAtRoute(result, projection, routeName, routeValues, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull =>
        MapCreatedAtRoute(result, projection, routeName, routeValuesSelector, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull =>
        MapCreatedAtRoute(result, routeName, routeValues, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TValue>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull =>
        MapCreatedAtRoute(result, routeName, routeValuesSelector, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull =>
        MapCreatedAtRoute(result, projection, routeName, routeValues, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull =>
        MapCreatedAtRoute(result, projection, routeName, routeValuesSelector, errorMapper);

    private static Results<CreatedAtRoute<TValue>, ProblemHttpResult> MapCreatedAtRoute<TValue>(
        Result<TValue> result,
        string? routeName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TValue>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(value, routeName, routeValues),
            error => MapProblem(error, errorMapper));
    }

    private static Results<CreatedAtRoute<TValue>, ProblemHttpResult> MapCreatedAtRoute<TValue>(
        Result<TValue> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TValue>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(value, routeName, routeValuesSelector(value)),
            error => MapProblem(error, errorMapper));
    }

    private static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> MapCreatedAtRoute<TValue, TResponse>(
        Result<TValue> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TResponse>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(Project(value, projection), routeName, routeValues),
            error => MapProblem(error, errorMapper));
    }

    private static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> MapCreatedAtRoute<TValue, TResponse>(
        Result<TValue> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TResponse>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(
                Project(value, projection),
                routeName,
                routeValuesSelector(value)),
            error => MapProblem(error, errorMapper));
    }

    private static Results<CreatedAtRoute<TValue>, ProblemHttpResult> MapCreatedAtRoute<TValue, TError>(
        Result<TValue, TError> result,
        string? routeName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TValue>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(value, routeName, routeValues),
            error => MapProblem(error, errorMapper));
    }

    private static Results<CreatedAtRoute<TValue>, ProblemHttpResult> MapCreatedAtRoute<TValue, TError>(
        Result<TValue, TError> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TValue>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(value, routeName, routeValuesSelector(value)),
            error => MapProblem(error, errorMapper));
    }

    private static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> MapCreatedAtRoute<TValue, TError, TResponse>(
        Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TResponse>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(Project(value, projection), routeName, routeValues),
            error => MapProblem(error, errorMapper));
    }

    private static Results<CreatedAtRoute<TResponse>, ProblemHttpResult> MapCreatedAtRoute<TValue, TError, TResponse>(
        Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return result.Match<Results<CreatedAtRoute<TResponse>, ProblemHttpResult>>(
            value => TypedResults.CreatedAtRoute(
                Project(value, projection),
                routeName,
                routeValuesSelector(value)),
            error => MapProblem(error, errorMapper));
    }
}
