namespace SecNetData.Services;

/// <summary>
/// Provides application-level information available throughout SecNet services.
/// Enables access to ApplicationName, Version, and Environment for logging, diagnostics, and API identification.
/// </summary>
public interface ISecApplicationInfo
{
    /// <summary>
    /// Gets the application name as configured in SecOptions.
    /// Used for logging, diagnostics, schema metadata, and future audit functionality.
    /// </summary>
    string ApplicationName { get; }

    /// <summary>
    /// Gets the SecNet framework version.
    /// </summary>
    string SecNetVersion { get; }

    /// <summary>
    /// Gets the ASP.NET Core environment (Development, Staging, Production, etc.).
    /// </summary>
    string Environment { get; }
}
