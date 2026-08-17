using System;
using System.Collections.Generic;
using System.Linq;

namespace SecNetData.Schema;

/// <summary>
/// Represents the expected schema metadata for a database table.
/// Derived from entity metadata.
/// </summary>
public sealed class SecTableMetadata
{
    /// <summary>
    /// The expected table name in MySQL.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// All columns expected in this table.
    /// </summary>
    public List<SecColumnMetadata> Columns { get; set; } = new();

    /// <summary>
    /// The primary key column, if any.
    /// </summary>
    public SecColumnMetadata? PrimaryKeyColumn { get; set; }

    /// <summary>
    /// Creates table metadata from entity metadata.
    /// </summary>
    public static SecTableMetadata FromEntityMetadata(
        SecEntityMetadata entityMetadata,
        MySQLTypeMapper typeMapper)
    {
        if (entityMetadata == null)
            throw new ArgumentNullException(nameof(entityMetadata));

        if (typeMapper == null)
            throw new ArgumentNullException(nameof(typeMapper));

        var tableMetadata = new SecTableMetadata
        {
            TableName = entityMetadata.TableName,
        };

        foreach (var propMetadata in entityMetadata.Properties)
        {
            var columnMetadata = SecColumnMetadata.FromPropertyMetadata(
                propMetadata,
                typeMapper);

            tableMetadata.Columns.Add(columnMetadata);

            if (columnMetadata.IsPrimaryKey)
            {
                tableMetadata.PrimaryKeyColumn = columnMetadata;
            }
        }

        return tableMetadata;
    }

    /// <summary>
    /// Gets the column metadata by column name.
    /// </summary>
    public SecColumnMetadata? GetColumn(string columnName)
    {
        return Columns.FirstOrDefault(c =>
            c.ColumnName.Equals(columnName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Validates the table metadata for schema generation.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(TableName))
        {
            errors.Add("TableName cannot be empty.");
        }

        if (Columns.Count == 0)
        {
            errors.Add($"Table '{TableName}' must have at least one column.");
        }

        // Check for duplicate column names
        var duplicateColumns = Columns
            .GroupBy(c => c.ColumnName, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var colName in duplicateColumns)
        {
            errors.Add($"Duplicate column name: '{colName}'");
        }

        return errors;
    }
}
