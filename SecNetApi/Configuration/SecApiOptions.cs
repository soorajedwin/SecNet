namespace SecNetApi.Configuration;

/// <summary>
/// Configuration options for the SecNet API layer.
/// </summary>
public sealed class SecApiOptions
{
    /// <summary>
    /// The route prefix for all SecNet entity endpoints.
    /// Default: "api/sec"
    /// </summary>
    public string RoutePrefix { get; set; } = "api/sec";

    /// <summary>
    /// The maximum allowed page size for list endpoints.
    /// Requests exceeding this value will be rejected with HTTP 400.
    /// Default: 100
    /// </summary>
    public int MaxPageSize { get; set; } = 100;

    /// <summary>
    /// When true, additional non-sensitive detail may be included in error responses.
    /// Never exposes stack traces, connection strings, or SQL regardless of this setting.
    /// Default: false
    /// </summary>
    public bool EnableDevelopmentErrors { get; set; } = false;

    /// <summary>
    /// When true, SecNet endpoints are included in OpenAPI output.
    /// Default: true
    /// </summary>
    public bool EnableOpenApi { get; set; } = true;
}
