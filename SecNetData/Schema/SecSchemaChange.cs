using System;

namespace SecNetData.Schema;

/// <summary>
/// Represents a single schema change applied or detected during synchronization.
/// </summary>
public sealed class SecSchemaChange
{
    /// <summary>
    /// Timestamp when the change was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The entity/class name affected (if applicable).
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// The table name affected (if applicable).
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// The column name affected (if applicable).
    /// </summary>
    public string? ColumnName { get; set; }

    /// <summary>
    /// The type of change that occurred.
    /// </summary>
    public SecSchemaChangeType ChangeType { get; set; }

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Whether this change represents a successful operation.
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// Optional error message if the change failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Enumerates the types of schema changes.
/// </summary>
public enum SecSchemaChangeType
{
    /// <summary>
    /// Database was created.
    /// </summary>
    DatabaseCreated,

    /// <summary>
    /// Table was created.
    /// </summary>
    TableCreated,

    /// <summary>
    /// Column was added to a table.
    /// </summary>
    ColumnAdded,

    /// <summary>
    /// Primary key was created/configured.
    /// </summary>
    PrimaryKeyCreated,

    /// <summary>
    /// Index was created.
    /// </summary>
    IndexCreated,

    /// <summary>
    /// A difference was detected but not applied (e.g., column rename).
    /// </summary>
    UnsupportedChange,

    /// <summary>
    /// Informational warning about the schema.
    /// </summary>
    Warning,

    /// <summary>
    /// An error occurred during schema synchronization.
    /// </summary>
    Error,
}
