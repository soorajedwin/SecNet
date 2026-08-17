using SecNetCore.Results;

namespace SecNetApi.Services;

/// <summary>
/// Maps SecResult objects to ASP.NET Core IResult HTTP responses with appropriate status codes.
/// Provides a single, consistent place for HTTP status code decisions across all SecNet endpoints.
/// </summary>
public sealed class SecHttpResultMapper
{
    /// <summary>
    /// Maps a SecResult to an IResult with HTTP 200 on success.
    /// </summary>
    public IResult Map<T>(SecResult<T> result)
        => MapWithStatus(result, StatusCodes.Status200OK);

    /// <summary>
    /// Maps a SecResult to an IResult with HTTP 201 Created on success.
    /// Includes a Location header pointing to the new resource.
    /// </summary>
    public IResult MapCreated<T>(SecResult<T> result, string locationUri)
    {
        if (result.Success)
            return Results.Created(locationUri, result);

        return Results.Json(result, statusCode: DetermineFailureStatus(result.Message));
    }

    /// <summary>
    /// Maps a SecResult to an IResult with HTTP 204 No Content on success.
    /// </summary>
    public IResult MapNoContent<T>(SecResult<T> result)
    {
        if (result.Success)
            return Results.NoContent();

        return Results.Json(result, statusCode: DetermineFailureStatus(result.Message));
    }

    private IResult MapWithStatus<T>(SecResult<T> result, int successStatus)
    {
        if (result.Success)
            return Results.Json(result, statusCode: successStatus);

        return Results.Json(result, statusCode: DetermineFailureStatus(result.Message));
    }

    /// <summary>
    /// Determines the appropriate HTTP failure status code from a SecResult message.
    /// </summary>
    public int DetermineFailureStatus(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return StatusCodes.Status500InternalServerError;

        // 404 — unknown entity or record not found
        if (message.StartsWith("Unknown entity", StringComparison.OrdinalIgnoreCase)
            || message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status404NotFound;

        // 403 — operation not permitted on a known entity
        if (message.Contains("not allowed", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status403Forbidden;

        // 400 — bad input: invalid ID, validation, property mapping, invalid sort, page size
        if (message.StartsWith("Invalid ID", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Invalid sort", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Invalid page", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Validation", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Property mapping", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Entity validation", StringComparison.OrdinalIgnoreCase)
            || message.StartsWith("Invalid request", StringComparison.OrdinalIgnoreCase))
            return StatusCodes.Status400BadRequest;

        return StatusCodes.Status500InternalServerError;
    }

    /// <summary>
    /// Creates a standard 404 SecResult response for an unknown entity name.
    /// </summary>
    public IResult EntityNotFound(string entityName)
    {
        var result = SecResult<object?>.Fail($"Unknown entity: '{entityName}'");
        return Results.Json(result, statusCode: StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Creates a standard 400 SecResult response for an invalid request.
    /// </summary>
    public IResult BadRequest(string message)
    {
        var result = SecResult<object?>.Fail(message);
        return Results.Json(result, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Creates a standard 500 SecResult response for an unexpected server error.
    /// </summary>
    public IResult ServerError(string message)
    {
        var result = SecResult<object?>.Fail(message);
        return Results.Json(result, statusCode: StatusCodes.Status500InternalServerError);
    }
}
