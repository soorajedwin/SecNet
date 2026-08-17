namespace SecNetData.Configuration;

/// <summary>
/// Configuration options for automatic schema synchronization behavior.
/// </summary>
public sealed class SecSchemaOptions
{
    /// <summary>
    /// The database name. Should match SecDatabaseOptions.DatabaseName.
    /// </summary>
    public string DatabaseName { get; set; } = string.Empty;

    /// <summary>
    /// Whether to automatically create the database if it doesn't exist.
    /// Default: true (safe - creates necessary infrastructure).
    /// </summary>
    public bool AutoCreateDatabase { get; set; } = true;

    /// <summary>
    /// Whether to automatically create tables for discovered SecEntity types.
    /// Default: true (safe - creates tables from models).
    /// </summary>
    public bool AutoCreateTables { get; set; } = true;

    /// <summary>
    /// Whether to automatically add missing columns to existing tables.
    /// Default: true (safe - only adds, never removes).
    /// </summary>
    public bool AutoAddColumns { get; set; } = true;

    /// <summary>
    /// Whether to automatically create indexes.
    /// Default: false (deferred for future implementation).
    /// </summary>
    public bool AutoCreateIndexes { get; set; } = false;

    /// <summary>
    /// Whether to run schema synchronization automatically on application startup.
    /// Default: true (recommended for development/testing).
    /// Consider setting to false in production with manual sync.
    /// </summary>
    public bool AutoSynchronizeOnStartup { get; set; } = true;
}
