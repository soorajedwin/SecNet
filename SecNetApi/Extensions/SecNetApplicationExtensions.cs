using Microsoft.Extensions.Logging;
using SecNetData.Services;

namespace SecNetApi.Extensions;

/// <summary>
/// Extension methods for SecNet application startup and initialization.
/// </summary>
public static class SecNetApplicationExtensions
{
    /// <summary>
    /// Initializes SecNet framework during application startup.
    /// This method executes the complete startup lifecycle:
    /// - Configuration validation
    /// - Entity discovery
    /// - Schema synchronization (if enabled)
    /// - Health check initialization
    /// 
    /// Must be called after AddSecNet() during application startup, before MapSecEndpoints().
    /// Throws InvalidOperationException if configuration is invalid or initialization fails.
    /// </summary>
    /// <param name="app">The WebApplication instance.</param>
    /// <example>
    /// <code>
    /// var builder = WebApplication.CreateBuilder(args);
    /// builder.Services.AddSecNet(options => { /* configure */ });
    /// 
    /// var app = builder.Build();
    /// 
    /// // Initialize SecNet before mapping endpoints
    /// await app.UseSecNetAsync();
    /// 
    /// app.MapSecEndpoints();
    /// app.Run();
    /// </code>
    /// </example>
    public static async Task UseSecNetAsync(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<WebApplication>>();
        logger.LogInformation("Starting SecNet initialization");

        // Get the startup orchestrator
        var orchestrator = app.Services.GetRequiredService<SecStartupOrchestrator>();

        try
        {
            // Execute the complete initialization sequence
            await orchestrator.InitializeAsync();
            logger.LogInformation("SecNet initialization completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "SecNet initialization failed");
            throw;
        }
    }

    /// <summary>
    /// Synchronous wrapper for UseSecNetAsync() for compatibility with synchronous startup patterns.
    /// Blocks until initialization completes.
    /// </summary>
    public static void UseSecNet(this WebApplication app)
    {
        // Use GetAwaiter().GetResult() for safe sync-over-async pattern in startup
        app.UseSecNetAsync().GetAwaiter().GetResult();
    }
}
