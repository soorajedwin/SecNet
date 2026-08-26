using Microsoft.Extensions.Hosting;

namespace SecNetData.Services;

/// <summary>
/// Implementation of ISecApplicationInfo providing application metadata.
/// </summary>
public sealed class SecApplicationInfo : ISecApplicationInfo
{
    private readonly string _applicationName;
    private readonly string _environment;

    /// <summary>
    /// Creates a new SecApplicationInfo instance.
    /// </summary>
    /// <param name="applicationName">The application name from SecOptions.</param>
    /// <param name="environment">The ASP.NET Core environment (IHostEnvironment).</param>
    public SecApplicationInfo(string applicationName, IHostEnvironment environment)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("Application name cannot be null or empty.", nameof(applicationName));
        if (environment == null)
            throw new ArgumentNullException(nameof(environment));

        _applicationName = applicationName;
        _environment = environment.EnvironmentName;
    }

    /// <summary>
    /// Gets the application name.
    /// </summary>
    public string ApplicationName => _applicationName;

    /// <summary>
    /// Gets the SecNet framework version.
    /// Currently returns a fixed version; can be enhanced to read from assembly version.
    /// </summary>
    public string SecNetVersion => "1.0.0";

    /// <summary>
    /// Gets the ASP.NET Core environment.
    /// </summary>
    public string Environment => _environment;
}
