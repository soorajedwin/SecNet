using Microsoft.Extensions.Logging;
using SecNetData.Configuration;
using SecNetData.Schema;

namespace SecNetData.Services;

/// <summary>
/// Orchestrates the complete SecNet startup lifecycle.
/// Coordinates validation, entity discovery, and schema initialization in the proper sequence.
/// </summary>
public sealed class SecStartupOrchestrator
{
    private readonly SecOptions _options;
    private readonly SecEntityDiscovery _entityDiscovery;
    private readonly ISecSchemaManager _schemaManager;
    private readonly ISecApplicationInfo _appInfo;
    private readonly ILogger<SecStartupOrchestrator>? _logger;

    /// <summary>
    /// Creates a new SecStartupOrchestrator instance.
    /// </summary>
    public SecStartupOrchestrator(
        SecOptions options,
        SecEntityDiscovery entityDiscovery,
        ISecSchemaManager schemaManager,
        ISecApplicationInfo appInfo,
        ILogger<SecStartupOrchestrator>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _entityDiscovery = entityDiscovery ?? throw new ArgumentNullException(nameof(entityDiscovery));
        _schemaManager = schemaManager ?? throw new ArgumentNullException(nameof(schemaManager));
        _appInfo = appInfo ?? throw new ArgumentNullException(nameof(appInfo));
        _logger = logger;
    }

    /// <summary>
    /// Executes the complete SecNet startup lifecycle.
    /// Sequence:
    /// 1. Validate configuration
    /// 2. Log initialization started
    /// 3. Discover entity assemblies
    /// 4. Log discovered entities
    /// 5. Synchronize schema (if AutoSynchronizeOnStartup is enabled)
    /// 6. Log completion
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for long-running operations.</param>
    /// <exception cref="InvalidOperationException">When configuration is invalid or initialization fails.</exception>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("SecNet initialization started for application: {ApplicationName}", _appInfo.ApplicationName);

        // Step 1: Validate configuration
        try
        {
            SecOptionsValidator.Validate(_options);
            _logger?.LogDebug("SecNet configuration validation passed");
        }
        catch (InvalidOperationException ex)
        {
            _logger?.LogError(ex, "SecNet configuration validation failed");
            throw;
        }

        // Step 2: Discover entities
        var entities = _entityDiscovery.GetEntities();
        _logger?.LogInformation(
            "Discovered {EntityCount} SecEntity types in {AssemblyCount} assemblies",
            entities.Count,
            _options.Database.DatabaseName);

        // Step 3: Synchronize schema if configured
        if (_options.Schema.AutoSynchronizeOnStartup)
        {
            _logger?.LogInformation("Starting schema synchronization for database: {DatabaseName}",
                _options.Database.DatabaseName);

            try
            {
                var result = await _schemaManager.SynchronizeAsync(cancellationToken);

                if (result.Success)
                {
                    _logger?.LogInformation(
                        "Schema synchronization completed successfully. Changes: {ChangeCount}",
                        result.Changes.Count);
                }
                else
                {
                    var message = $"Schema synchronization failed: {result.Message}";
                    _logger?.LogError(message);
                    throw new InvalidOperationException(message);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Schema synchronization failed");
                throw;
            }
        }
        else
        {
            _logger?.LogInformation("Schema synchronization is disabled (AutoSynchronizeOnStartup = false)");
        }

        _logger?.LogInformation("SecNet initialization completed successfully");
    }
}
