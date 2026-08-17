using System.Text.Json;
using Microsoft.Extensions.Hosting;
using SecNetCore.Results;
using SecNetApi.Configuration;

namespace SecNetApi.Middleware;

/// <summary>
/// Global exception middleware for the SecNet API.
/// Catches unhandled exceptions and returns a safe, standard SecResult error response.
/// Never exposes stack traces, SQL, connection strings, or internal type names to clients.
/// </summary>
public sealed class SecExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecExceptionMiddleware> _logger;
    private readonly SecApiOptions _options;
    private readonly IHostEnvironment _environment;

    public SecExceptionMiddleware(
        RequestDelegate next,
        ILogger<SecExceptionMiddleware> logger,
        SecApiOptions options,
        IHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing request {Method} {Path}",
                context.Request.Method, context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            _logger.LogWarning("Response has already started; cannot write exception response.");
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        var isDevelopment = _environment.IsDevelopment() && _options.EnableDevelopmentErrors;

        // Only include the exception type name in development — never the full message or stack trace
        var message = isDevelopment
            ? $"An unexpected error occurred ({ex.GetType().Name})."
            : "An unexpected error occurred. Please try again later.";

        var result = new SecResult<object?>
        {
            Success = false,
            Message = message,
            Errors = new List<string> { message }
        };

        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json, context.RequestAborted);
    }
}
