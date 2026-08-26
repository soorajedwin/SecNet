using System.Reflection;

namespace SecNetData.Configuration;

/// <summary>
/// Central unified configuration for the entire SecNet framework.
/// Contains all configuration needed for a complete SecNet integration.
/// </summary>
public sealed class SecOptions
{
    /// <summary>
    /// The application name. Used for logging, diagnostics, schema metadata, and future audit functionality.
    /// Required: must not be empty.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// Database configuration (server, port, credentials, etc.).
    /// Reuses SecDatabaseOptions for consistency.
    /// Required: Server, Username, and DatabaseName must be provided.
    /// </summary>
    public SecDatabaseOptions Database { get; set; } = new();

    /// <summary>
    /// Schema synchronization configuration options.
    /// Controls automatic database and table creation, column addition, and synchronization timing.
    /// </summary>
    public SecSchemaOptions Schema { get; set; } = new();

    /// <summary>
    /// CRUD engine configuration options.
    /// Controls default and maximum page sizes.
    /// </summary>
    public SecCrudOptions Crud { get; set; } = new();

    /// <summary>
    /// Assemblies containing [SecEntity] models.
    /// The application must explicitly register the assemblies where entity types are defined.
    /// Example: options.EntityAssemblies.Add(typeof(Customer).Assembly)
    /// </summary>
    public IList<Assembly> EntityAssemblies { get; set; } = new List<Assembly>();
}