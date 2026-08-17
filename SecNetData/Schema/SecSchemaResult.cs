using System;
using System.Collections.Generic;
using System.Linq;

namespace SecNetData.Schema;

/// <summary>
/// Represents the result of a schema synchronization operation.
/// </summary>
public sealed class SecSchemaResult
{
    /// <summary>
    /// Whether the synchronization completed successfully.
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Overall message summary.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when synchronization started.
    /// </summary>
    public DateTime StartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when synchronization completed.
    /// </summary>
    public DateTime EndTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// All schema changes detected or applied.
    /// </summary>
    public List<SecSchemaChange> Changes { get; set; } = new();

    /// <summary>
    /// Gets the duration of the synchronization.
    /// </summary>
    public TimeSpan Duration => EndTime - StartTime;

    /// <summary>
    /// Count of databases created.
    /// </summary>
    public int DatabasesCreated => 
        Changes.Count(c => c.ChangeType == SecSchemaChangeType.DatabaseCreated && c.IsSuccess);

    /// <summary>
    /// Count of tables created.
    /// </summary>
    public int TablesCreated =>
        Changes.Count(c => c.ChangeType == SecSchemaChangeType.TableCreated && c.IsSuccess);

    /// <summary>
    /// Count of columns added.
    /// </summary>
    public int ColumnsAdded =>
        Changes.Count(c => c.ChangeType == SecSchemaChangeType.ColumnAdded && c.IsSuccess);

    /// <summary>
    /// Count of indexes created.
    /// </summary>
    public int IndexesCreated =>
        Changes.Count(c => c.ChangeType == SecSchemaChangeType.IndexCreated && c.IsSuccess);

    /// <summary>
    /// Count of unsupported changes detected.
    /// </summary>
    public int UnsupportedChanges =>
        Changes.Count(c => c.ChangeType == SecSchemaChangeType.UnsupportedChange);

    /// <summary>
    /// Count of warnings.
    /// </summary>
    public int Warnings =>
        Changes.Count(c => c.ChangeType == SecSchemaChangeType.Warning);

    /// <summary>
    /// Count of errors.
    /// </summary>
    public int Errors =>
        Changes.Count(c => c.ChangeType == SecSchemaChangeType.Error);

    /// <summary>
    /// Gets all error changes.
    /// </summary>
    public IEnumerable<SecSchemaChange> ErrorChanges =>
        Changes.Where(c => c.ChangeType == SecSchemaChangeType.Error || !c.IsSuccess);

    /// <summary>
    /// Gets all warning changes.
    /// </summary>
    public IEnumerable<SecSchemaChange> WarningChanges =>
        Changes.Where(c => c.ChangeType == SecSchemaChangeType.Warning);

    /// <summary>
    /// Adds a change record.
    /// </summary>
    public void AddChange(SecSchemaChange change)
    {
        Changes.Add(change);
    }

    /// <summary>
    /// Creates a successful result with a message.
    /// </summary>
    public static SecSchemaResult Ok(string message = "Schema synchronization completed successfully.")
    {
        return new SecSchemaResult
        {
            Success = true,
            Message = message,
        };
    }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static SecSchemaResult Fail(string message, string? errorDetails = null)
    {
        return new SecSchemaResult
        {
            Success = false,
            Message = message,
        };
    }

    /// <summary>
    /// Generates a summary report of all changes.
    /// </summary>
    public string GetSummary()
    {
        var lines = new List<string>
        {
            $"Schema Synchronization Result",
            $"============================",
            $"Success: {Success}",
            $"Duration: {Duration.TotalMilliseconds:F0}ms",
            $"",
            $"Changes Applied:",
            $"  - Databases Created: {DatabasesCreated}",
            $"  - Tables Created: {TablesCreated}",
            $"  - Columns Added: {ColumnsAdded}",
            $"  - Indexes Created: {IndexesCreated}",
            $"",
            $"Warnings: {Warnings}",
            $"Unsupported Changes: {UnsupportedChanges}",
            $"Errors: {Errors}",
        };

        if (Changes.Count > 0)
        {
            lines.Add("");
            lines.Add("Details:");
            foreach (var change in Changes)
            {
                lines.Add($"  [{change.ChangeType}] {change.Description}");
                if (!string.IsNullOrWhiteSpace(change.ErrorMessage))
                {
                    lines.Add($"    Error: {change.ErrorMessage}");
                }
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
