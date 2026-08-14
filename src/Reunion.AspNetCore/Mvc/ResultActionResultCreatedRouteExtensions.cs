using Microsoft.AspNetCore.Mvc;
using Reunion.Errors;

namespace Reunion.AspNetCore.Mvc;

public static partial class ResultActionResultExtensions
{
    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName)
        where TValue : notnull
        where TError : IError =>
        result.ToCreatedAtActionOrProblem(actionName, routeValues: null, controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        object? routeValues)
        where TValue : notnull
        where TError : IError =>
        result.ToCreatedAtActionOrProblem(actionName, routeValues, controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        object? routeValues,
        string? controllerName)
        where TValue : notnull
        where TError : IError
    {
        ValidateActionName(actionName);
        return result.ToActionResult<TValue, TError, TValue>(
            value => CreateAtAction(value, actionName, controllerName, routeValues));
    }

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        Func<TValue, object?> routeValuesSelector)
        where TValue : notnull
        where TError : IError =>
        result.ToCreatedAtActionOrProblem(
            actionName,
            routeValuesSelector,
            controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        string? controllerName)
        where TValue : notnull
        where TError : IError
    {
        ValidateAction(actionName, routeValuesSelector);
        return result.ToActionResult<TValue, TError, TValue>(
            value => CreateAtAction(value, actionName, controllerName, routeValuesSelector));
    }

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull =>
        result.ToCreatedAtActionOrProblem(
            projection,
            actionName,
            routeValues: null,
            controllerName: null);

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        object? routeValues)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull =>
        result.ToCreatedAtActionOrProblem(
            projection,
            actionName,
            routeValues,
            controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        object? routeValues,
        string? controllerName)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ValidateAction(actionName, projection);
        return result.ToActionResult<TValue, TError, TResponse>(
            value => CreateAtAction(value, projection, actionName, controllerName, routeValues));
    }

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        Func<TValue, object?> routeValuesSelector)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull =>
        result.ToCreatedAtActionOrProblem(
            projection,
            actionName,
            routeValuesSelector,
            controllerName: null);

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        string? controllerName)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ValidateAction(actionName, projection, routeValuesSelector);
        return result.ToActionResult<TValue, TError, TResponse>(
            value => CreateAtAction(
                value,
                projection,
                actionName,
                controllerName,
                routeValuesSelector));
    }

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue>(
        this Result<TValue> result,
        string actionName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull =>
        result.ToCreatedAtActionOrProblem(
            actionName,
            routeValues,
            errorMapper,
            controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue>(
        this Result<TValue> result,
        string actionName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
    {
        ValidateAction(actionName, errorMapper);
        return MapActionResult<TValue, TValue>(
            result,
            value => CreateAtAction(value, actionName, controllerName, routeValues),
            errorMapper);
    }

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue>(
        this Result<TValue> result,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull =>
        result.ToCreatedAtActionOrProblem(
            actionName,
            routeValuesSelector,
            errorMapper,
            controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue>(
        this Result<TValue> result,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
    {
        ValidateAction(actionName, routeValuesSelector, errorMapper);
        return MapActionResult<TValue, TValue>(
            result,
            value => CreateAtAction(value, actionName, controllerName, routeValuesSelector),
            errorMapper);
    }

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string actionName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull =>
        result.ToCreatedAtActionOrProblem(
            projection,
            actionName,
            routeValues,
            errorMapper,
            controllerName: null);

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string actionName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
        where TResponse : notnull
    {
        ValidateAction(actionName, projection, errorMapper);
        return MapActionResult<TValue, TResponse>(
            result,
            value => CreateAtAction(value, projection, actionName, controllerName, routeValues),
            errorMapper);
    }

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull =>
        result.ToCreatedAtActionOrProblem(
            projection,
            actionName,
            routeValuesSelector,
            errorMapper,
            controllerName: null);

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
        where TResponse : notnull
    {
        ValidateAction(actionName, projection, routeValuesSelector, errorMapper);
        return MapActionResult<TValue, TResponse>(
            result,
            value => CreateAtAction(
                value,
                projection,
                actionName,
                controllerName,
                routeValuesSelector),
            errorMapper);
    }

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull =>
        result.ToCreatedAtActionOrProblem(
            actionName,
            routeValues,
            errorMapper,
            controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
        where TError : notnull
    {
        ValidateAction(actionName, errorMapper);
        return MapActionResult<TValue, TError, TValue>(
            result,
            value => CreateAtAction(value, actionName, controllerName, routeValues),
            errorMapper);
    }

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull =>
        result.ToCreatedAtActionOrProblem(
            actionName,
            routeValuesSelector,
            errorMapper,
            controllerName: null);

    /// <summary>Maps success to CreatedAtAction and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtActionOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
        where TError : notnull
    {
        ValidateAction(actionName, routeValuesSelector, errorMapper);
        return MapActionResult<TValue, TError, TValue>(
            result,
            value => CreateAtAction(value, actionName, controllerName, routeValuesSelector),
            errorMapper);
    }

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull =>
        result.ToCreatedAtActionOrProblem(
            projection,
            actionName,
            routeValues,
            errorMapper,
            controllerName: null);

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull
    {
        ValidateAction(actionName, projection, errorMapper);
        return MapActionResult<TValue, TError, TResponse>(
            result,
            value => CreateAtAction(value, projection, actionName, controllerName, routeValues),
            errorMapper);
    }

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull =>
        result.ToCreatedAtActionOrProblem(
            projection,
            actionName,
            routeValuesSelector,
            errorMapper,
            controllerName: null);

    /// <summary>Projects success to CreatedAtAction and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtActionOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string actionName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper,
        string? controllerName)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull
    {
        ValidateAction(actionName, projection, routeValuesSelector, errorMapper);
        return MapActionResult<TValue, TError, TResponse>(
            result,
            value => CreateAtAction(
                value,
                projection,
                actionName,
                controllerName,
                routeValuesSelector),
            errorMapper);
    }

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result)
        where TValue : notnull
        where TError : IError =>
        result.ToCreatedAtRouteOrProblem(routeName: null, routeValues: null);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName)
        where TValue : notnull
        where TError : IError =>
        result.ToCreatedAtRouteOrProblem(routeName, routeValues: null);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        object? routeValues)
        where TValue : notnull
        where TError : IError =>
        result.ToActionResult<TValue, TError, TValue>(
            value => CreateAtRoute(value, routeName, routeValues));

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector)
        where TValue : notnull
        where TError : IError
    {
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        return result.ToActionResult<TValue, TError, TValue>(
            value => CreateAtRoute(value, routeName, routeValuesSelector));
    }

    /// <summary>Projects success to CreatedAtRoute and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull =>
        result.ToCreatedAtRouteOrProblem(projection, routeName: null, routeValues: null);

    /// <summary>Projects success to CreatedAtRoute and maps failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull =>
        result.ToCreatedAtRouteOrProblem(projection, routeName, routeValues: null);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues)
        where TValue : notnull
        where TError : IError
        where TResponse : notnull
    {
        ArgumentNullException.ThrowIfNull(projection);
        return result.ToActionResult<TValue, TError, TResponse>(
            value => CreateAtRoute(value, projection, routeName, routeValues));
    }

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
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
        return result.ToActionResult<TValue, TError, TResponse>(
            value => CreateAtRoute(value, projection, routeName, routeValuesSelector));
    }

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue>(
        this Result<TValue> result,
        string? routeName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull =>
        MapCreatedAtRoute(result, routeName, routeValues, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue>(
        this Result<TValue> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull =>
        MapCreatedAtRoute(result, routeName, routeValuesSelector, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull =>
        MapCreatedAtRoute(result, projection, routeName, routeValues, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TResponse>(
        this Result<TValue> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull =>
        MapCreatedAtRoute(result, projection, routeName, routeValuesSelector, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull =>
        MapCreatedAtRoute(result, routeName, routeValues, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TValue> ToCreatedAtRouteOrProblem<TValue, TError>(
        this Result<TValue, TError> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull =>
        MapCreatedAtRoute(result, routeName, routeValuesSelector, errorMapper);

    /// <summary>Maps success to CreatedAtRoute and failure to ProblemDetails.</summary>
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
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
    public static ActionResult<TResponse> ToCreatedAtRouteOrProblem<TValue, TError, TResponse>(
        this Result<TValue, TError> result,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull =>
        MapCreatedAtRoute(result, projection, routeName, routeValuesSelector, errorMapper);

    private static ActionResult<TValue> MapCreatedAtRoute<TValue>(
        Result<TValue> result,
        string? routeName,
        object? routeValues,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return MapActionResult<TValue, TValue>(
            result,
            value => CreateAtRoute(value, routeName, routeValues),
            errorMapper);
    }

    private static ActionResult<TValue> MapCreatedAtRoute<TValue>(
        Result<TValue> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
    {
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return MapActionResult<TValue, TValue>(
            result,
            value => CreateAtRoute(value, routeName, routeValuesSelector),
            errorMapper);
    }

    private static ActionResult<TResponse> MapCreatedAtRoute<TValue, TResponse>(
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
        return MapActionResult<TValue, TResponse>(
            result,
            value => CreateAtRoute(value, projection, routeName, routeValues),
            errorMapper);
    }

    private static ActionResult<TResponse> MapCreatedAtRoute<TValue, TResponse>(
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
        return MapActionResult<TValue, TResponse>(
            result,
            value => CreateAtRoute(value, projection, routeName, routeValuesSelector),
            errorMapper);
    }

    private static ActionResult<TValue> MapCreatedAtRoute<TValue, TError>(
        Result<TValue, TError> result,
        string? routeName,
        object? routeValues,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(errorMapper);
        return MapActionResult<TValue, TError, TValue>(
            result,
            value => CreateAtRoute(value, routeName, routeValues),
            errorMapper);
    }

    private static ActionResult<TValue> MapCreatedAtRoute<TValue, TError>(
        Result<TValue, TError> result,
        string? routeName,
        Func<TValue, object?> routeValuesSelector,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
    {
        ArgumentNullException.ThrowIfNull(routeValuesSelector);
        ArgumentNullException.ThrowIfNull(errorMapper);
        return MapActionResult<TValue, TError, TValue>(
            result,
            value => CreateAtRoute(value, routeName, routeValuesSelector),
            errorMapper);
    }

    private static ActionResult<TResponse> MapCreatedAtRoute<TValue, TError, TResponse>(
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
        return MapActionResult<TValue, TError, TResponse>(
            result,
            value => CreateAtRoute(value, projection, routeName, routeValues),
            errorMapper);
    }

    private static ActionResult<TResponse> MapCreatedAtRoute<TValue, TError, TResponse>(
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
        return MapActionResult<TValue, TError, TResponse>(
            result,
            value => CreateAtRoute(value, projection, routeName, routeValuesSelector),
            errorMapper);
    }

    private static ActionResult<TResponse> MapActionResult<TValue, TError, TResponse>(
        Result<TValue, TError> result,
        Func<TValue, ActionResult<TResponse>> successMapper,
        Func<TError, ProblemDetails> errorMapper)
        where TValue : notnull
        where TError : notnull
        where TResponse : notnull =>
        result.Match<ActionResult<TResponse>>(
            value => MapSuccess(value, successMapper),
            error => errorMapper(error).ToObjectResult());

    private static ActionResult<TResponse> MapActionResult<TValue, TResponse>(
        Result<TValue> result,
        Func<TValue, ActionResult<TResponse>> successMapper,
        Func<string, ProblemDetails> errorMapper)
        where TValue : notnull
        where TResponse : notnull =>
        result.Match<ActionResult<TResponse>>(
            value => MapSuccess(value, successMapper),
            error => errorMapper(error).ToObjectResult());

    private static CreatedAtActionResult CreateAtAction<TValue>(
        TValue value,
        string actionName,
        string? controllerName,
        object? routeValues) =>
        new(actionName, controllerName, routeValues, value);

    private static CreatedAtActionResult CreateAtAction<TValue>(
        TValue value,
        string actionName,
        string? controllerName,
        Func<TValue, object?> routeValuesSelector) =>
        new(actionName, controllerName, routeValuesSelector(value), value);

    private static CreatedAtActionResult CreateAtAction<TValue, TResponse>(
        TValue value,
        Func<TValue, TResponse> projection,
        string actionName,
        string? controllerName,
        object? routeValues)
        where TResponse : notnull =>
        new(actionName, controllerName, routeValues, Project(value, projection));

    private static CreatedAtActionResult CreateAtAction<TValue, TResponse>(
        TValue value,
        Func<TValue, TResponse> projection,
        string actionName,
        string? controllerName,
        Func<TValue, object?> routeValuesSelector)
        where TResponse : notnull
    {
        var response = Project(value, projection);
        return new(
            actionName,
            controllerName,
            routeValuesSelector(value),
            response);
    }

    private static CreatedAtRouteResult CreateAtRoute<TValue>(
        TValue value,
        string? routeName,
        object? routeValues) =>
        new(routeName, routeValues, value);

    private static CreatedAtRouteResult CreateAtRoute<TValue>(
        TValue value,
        string? routeName,
        Func<TValue, object?> routeValuesSelector) =>
        new(routeName, routeValuesSelector(value), value);

    private static CreatedAtRouteResult CreateAtRoute<TValue, TResponse>(
        TValue value,
        Func<TValue, TResponse> projection,
        string? routeName,
        object? routeValues)
        where TResponse : notnull =>
        new(routeName, routeValues, Project(value, projection));

    private static CreatedAtRouteResult CreateAtRoute<TValue, TResponse>(
        TValue value,
        Func<TValue, TResponse> projection,
        string? routeName,
        Func<TValue, object?> routeValuesSelector)
        where TResponse : notnull
    {
        var response = Project(value, projection);
        return new(routeName, routeValuesSelector(value), response);
    }

    private static void ValidateActionName(string actionName) =>
        ArgumentException.ThrowIfNullOrWhiteSpace(actionName);

    private static void ValidateAction<T>(string actionName, T value)
    {
        ValidateActionName(actionName);
        ArgumentNullException.ThrowIfNull(value);
    }

    private static void ValidateAction<T1, T2>(string actionName, T1 first, T2 second)
    {
        ValidateAction(actionName, first);
        ArgumentNullException.ThrowIfNull(second);
    }

    private static void ValidateAction<T1, T2, T3>(
        string actionName,
        T1 first,
        T2 second,
        T3 third)
    {
        ValidateAction(actionName, first, second);
        ArgumentNullException.ThrowIfNull(third);
    }
}
