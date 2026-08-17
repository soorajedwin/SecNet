using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace SecNetData.Schema;

/// <summary>
/// Compares expected schema (from entity metadata) with actual schema (from database).
/// </summary>
public sealed class SecSchemaComparator
{
    private readonly ILogger<SecSchemaComparator>? _logger;

    public SecSchemaComparator(ILogger<SecSchemaComparator>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Compares expected table schema with existing columns.
    /// Returns information about missing columns and schema differences.
    /// </summary>
    public SecTableSchemaComparison CompareTableSchema(
        SecTableMetadata expectedTable,
        Dictionary<string, SecExistingColumn> existingColumns)
    {
        var comparison = new SecTableSchemaComparison
        {
            TableName = expectedTable.TableName,
            ExpectedColumnCount = expectedTable.Columns.Count,
            ActualColumnCount = existingColumns.Count,
        };

        _logger?.LogDebug("Comparing schema for table: {TableName}", expectedTable.TableName);

        // Find missing columns
        foreach (var expectedColumn in expectedTable.Columns)
        {
            var existing = existingColumns.FirstOrDefault(ec =>
                ec.Key.Equals(expectedColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (existing.Key == null)
            {
                comparison.MissingColumns.Add(expectedColumn);
                _logger?.LogDebug("Missing column in table {TableName}: {ColumnName}",
                    expectedTable.TableName, expectedColumn.ColumnName);
            }
            else
            {
                // Compare existing column definition
                var columnComparison = CompareColumnDefinitions(
                    expectedColumn,
                    existing.Value);

                if (!columnComparison.Matches)
                {
                    comparison.ColumnDifferences.Add(columnComparison);
                    _logger?.LogDebug("Column definition mismatch in table {TableName}: {ColumnName}",
                        expectedTable.TableName, expectedColumn.ColumnName);
                }
            }
        }

        // Find extra columns in database that are not in the model
        foreach (var existingColumn in existingColumns.Values)
        {
            var expected = expectedTable.Columns.FirstOrDefault(ec =>
                ec.ColumnName.Equals(existingColumn.ColumnName, StringComparison.OrdinalIgnoreCase));

            if (expected == null)
            {
                comparison.ExtraColumns.Add(existingColumn);
                _logger?.LogDebug("Extra column in table {TableName}: {ColumnName}",
                    expectedTable.TableName, existingColumn.ColumnName);
            }
        }

        comparison.IsSynchronized = comparison.MissingColumns.Count == 0 &&
                                    comparison.ColumnDifferences.Count == 0;

        return comparison;
    }

    /// <summary>
    /// Compares an expected column definition with an actual database column.
    /// </summary>
    private SecColumnDefinitionComparison CompareColumnDefinitions(
        SecColumnMetadata expected,
        SecExistingColumn actual)
    {
        var comparison = new SecColumnDefinitionComparison
        {
            ColumnName = expected.ColumnName,
            ExpectedType = expected.MySQLType,
            ActualType = actual.ColumnType,
        };

        // Check type match (simplified - just string comparison)
        // In a real scenario, you might want deeper type comparison
        comparison.Matches = expected.MySQLType.Equals(actual.ColumnType, StringComparison.OrdinalIgnoreCase);

        // Check nullability
        if (expected.IsNullable != actual.IsNullable)
        {
            comparison.Matches = false;
            comparison.Issues.Add(
                $"Nullability mismatch: expected nullable={expected.IsNullable}, actual nullable={actual.IsNullable}");
        }

        // Check primary key
        if (expected.IsPrimaryKey != actual.IsPrimaryKey)
        {
            comparison.Matches = false;
            comparison.Issues.Add(
                $"Primary key mismatch: expected isPrimaryKey={expected.IsPrimaryKey}, actual isPrimaryKey={actual.IsPrimaryKey}");
        }

        return comparison;
    }
}

/// <summary>
/// Represents the schema comparison result for a single table.
/// </summary>
public sealed class SecTableSchemaComparison
{
    /// <summary>
    /// The table name being compared.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// Expected column count from the model.
    /// </summary>
    public int ExpectedColumnCount { get; set; }

    /// <summary>
    /// Actual column count in the database.
    /// </summary>
    public int ActualColumnCount { get; set; }

    /// <summary>
    /// Columns expected in the model but missing from the database.
    /// </summary>
    public List<SecColumnMetadata> MissingColumns { get; set; } = new();

    /// <summary>
    /// Columns that exist in the database but are not in the model.
    /// </summary>
    public List<SecExistingColumn> ExtraColumns { get; set; } = new();

    /// <summary>
    /// Columns with definition mismatches (type, nullability, etc).
    /// </summary>
    public List<SecColumnDefinitionComparison> ColumnDifferences { get; set; } = new();

    /// <summary>
    /// Whether the schema is fully synchronized.
    /// </summary>
    public bool IsSynchronized { get; set; }
}

/// <summary>
/// Represents a comparison between expected and actual column definitions.
/// </summary>
public sealed class SecColumnDefinitionComparison
{
    /// <summary>
    /// The column name.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// Expected MySQL type from the model.
    /// </summary>
    public string ExpectedType { get; set; } = string.Empty;

    /// <summary>
    /// Actual MySQL type in the database.
    /// </summary>
    public string ActualType { get; set; } = string.Empty;

    /// <summary>
    /// Whether definitions match.
    /// </summary>
    public bool Matches { get; set; } = true;

    /// <summary>
    /// Issues found in the comparison.
    /// </summary>
    public List<string> Issues { get; set; } = new();
}
