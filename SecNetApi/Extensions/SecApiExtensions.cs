using SecNetApi.Configuration;
using SecNetApi.Endpoints;
using SecNetApi.Middleware;
using SecNetApi.Services;

namespace SecNetApi.Extensions;

/// <summary>
/// Extension methods for configuring the SecNet API layer.
/// </summary>
public static class SecApiExtensions
{
    /// <summary>
    /// Registers SecNet API services in the DI container.
    /// Call this before <see cref="MapSecEndpoints"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for SecNet API options.</param>
    public static IServiceCollection AddSecApi(
        this IServiceCollection services,
        Action<SecApiOptions>? configure = null)
    {
        var options = new SecApiOptions();
        configure?.Invoke(options);

        services.AddSingleton(options);
        services.AddSingleton<SecHttpResultMapper>();

        return services;
    }

    /// <summary>
    /// Adds the SecNet exception middleware to the request pipeline.
    /// Call this early in the middleware pipeline (before routing) to catch all unhandled exceptions.
    /// </summary>
    public static IApplicationBuilder UseSecExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<SecExceptionMiddleware>();

    /// <summary>
    /// Maps the generic SecNet entity endpoints onto the application.
    /// Automatically discovers and serves all registered [SecEntity] types.
    /// </summary>
    /// <param name="app">The web application.</param>
    public static IEndpointRouteBuilder MapSecEndpoints(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<SecApiOptions>();
        app.MapSecEntityEndpoints(options);
        return app;
    }
}
