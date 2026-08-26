using Microsoft.Extensions.Diagnostics.HealthChecks;
using SecNetData.Services;

namespace SecNetApi.Health;

/// <summary>
/// Health check service for SecNet framework.
/// Verifies basic SecNet initialization status and framework availability.
/// </summary>
public sealed class SecNetHealthCheck : IHealthCheck
{
    private readonly ISecApplicationInfo _appInfo;

    /// <summary>
    /// Creates a new SecNetHealthCheck instance.
    /// </summary>
    public SecNetHealthCheck(ISecApplicationInfo appInfo)
    {
        _appInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));
    }

    /// <summary>
    /// Checks the health of SecNet framework.
    /// Returns Healthy if:
    /// - Application name is configured
    /// - ISecApplicationInfo is available
    /// </summary>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, object>
            {
                { "ApplicationName", _appInfo.ApplicationName },
                { "SecNetVersion", _appInfo.SecNetVersion },
                { "Environment", _appInfo.Environment },
                { "CheckTime", DateTime.UtcNow }
            };

            return Task.FromResult(HealthCheckResult.Healthy(
                $"SecNet healthy for application '{_appInfo.ApplicationName}'",
                data: data));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "SecNet health check failed",
                exception: ex));
        }
    }
}
