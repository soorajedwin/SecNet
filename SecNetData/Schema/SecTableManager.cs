using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using SecNetData.Configuration;
using SecNetData.Services;

namespace SecNetData.Schema;

/// <summary>
/// Manages table-level MySQL operations (create, check existence, manage columns).
/// </summary>
public sealed class SecTableManager
{
    private readonly SecDatabaseOptions _options;
    private readonly SecConnectionStringBuilder _connectionBuilder;
    private readonly ILogger<SecTableManager>? _logger;

    public SecTableManager(
        SecDatabaseOptions options,
        SecConnectionStringBuilder connectionBuilder,
        ILogger<SecTableManager>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionBuilder = connectionBuilder ?? throw new ArgumentNullException(nameof(connectionBuilder));
        _logger = logger;
    }

    /// <summary>
    /// Checks whether a table exists in the database.
    /// </summary>
    public async Task<bool> TableExistsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var connectionString = _connectionBuilder.BuildDatabaseConnectionString(_options);

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT 1
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = @databaseName
                AND TABLE_NAME = @tableName
                LIMIT 1";

            command.Parameters.AddWithValue("@databaseName", _options.DatabaseName);
            command.Parameters.AddWithValue("@tableName", tableName);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking if table exists: {TableName}", tableName);
            throw;
        }
    }

    /// <summary>
    /// Gets existing columns for a table.
    /// </summary>
    public async Task<Dictionary<string, SecExistingColumn>> GetExistingColumnsAsync(
        string tableName,
        CancellationToken cancellationToken = default)
    {
        var columns = new Dictionary<string, SecExistingColumn>(StringComparer.OrdinalIgnoreCase);
        var connectionString = _connectionBuilder.BuildDatabaseConnectionString(_options);

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT
                    COLUMN_NAME,
                    COLUMN_TYPE,
                    IS_NULLABLE,
                    COLUMN_KEY,
                    EXTRA
                FROM information_schema.COLUMNS
                WHERE TABLE_SCHEMA = @databaseName
                AND TABLE_NAME = @tableName
                ORDER BY ORDINAL_POSITION";

            command.Parameters.AddWithValue("@databaseName", _options.DatabaseName);
            command.Parameters.AddWithValue("@tableName", tableName);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                var columnName = reader.GetString(0);
                var columnType = reader.GetString(1);
                var isNullable = reader.GetString(2) == "YES";
                var columnKey = reader.GetString(3);
                var extra = reader.GetString(4) ?? "";

                var column = new SecExistingColumn
                {
                    ColumnName = columnName,
                    ColumnType = columnType,
                    IsNullable = isNullable,
                    IsPrimaryKey = columnKey == "PRI",
                    IsAutoIncrement = extra.Contains("auto_increment", StringComparison.OrdinalIgnoreCase),
                };

                columns[columnName] = column;
            }

            return columns;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving columns for table: {TableName}", tableName);
            throw;
        }
    }

    /// <summary>
    /// Creates a table in the database.
    /// </summary>
    public async Task<SecSchemaResult> CreateTableAsync(
        SecTableMetadata tableMetadata,
        CancellationToken cancellationToken = default)
    {
        var result = SecSchemaResult.Ok();
        result.StartTime = DateTime.UtcNow;

        try
        {
            // Validate metadata
            var validationErrors = tableMetadata.Validate();
            if (validationErrors.Count > 0)
            {
                result.Success = false;
                result.Message = $"Invalid table metadata: {string.Join("; ", validationErrors)}";
                foreach (var error in validationErrors)
                {
                    result.AddChange(new SecSchemaChange
                    {
                        ChangeType = SecSchemaChangeType.Error,
                        TableName = tableMetadata.TableName,
                        Description = error,
                        IsSuccess = false,
                    });
                }
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            var connectionString = _connectionBuilder.BuildDatabaseConnectionString(_options);

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            // Build CREATE TABLE statement
            var createTableSql = BuildCreateTableSql(tableMetadata);

            _logger?.LogInformation("Creating table: {TableName}", tableMetadata.TableName);

            using var command = connection.CreateCommand();
            command.CommandText = createTableSql;

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger?.LogInformation("Table created successfully: {TableName}", tableMetadata.TableName);

            result.AddChange(new SecSchemaChange
            {
                TableName = tableMetadata.TableName,
                ChangeType = SecSchemaChangeType.TableCreated,
                Description = $"Table '{tableMetadata.TableName}' created with {tableMetadata.Columns.Count} columns",
            });

            if (tableMetadata.PrimaryKeyColumn != null)
            {
                result.AddChange(new SecSchemaChange
                {
                    TableName = tableMetadata.TableName,
                    ColumnName = tableMetadata.PrimaryKeyColumn.ColumnName,
                    ChangeType = SecSchemaChangeType.PrimaryKeyCreated,
                    Description = $"Primary key configured on '{tableMetadata.PrimaryKeyColumn.ColumnName}'",
                });
            }

            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating table: {TableName}", tableMetadata.TableName);
            result.Success = false;
            result.Message = $"Failed to create table: {ex.Message}";
            result.AddChange(new SecSchemaChange
            {
                ChangeType = SecSchemaChangeType.Error,
                TableName = tableMetadata.TableName,
                Description = $"Failed to create table: {ex.Message}",
                IsSuccess = false,
                ErrorMessage = ex.Message,
            });
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Adds missing columns to an existing table.
    /// </summary>
    public async Task<SecSchemaResult> AddMissingColumnsAsync(
        string tableName,
        List<SecColumnMetadata> missingColumns,
        CancellationToken cancellationToken = default)
    {
        var result = SecSchemaResult.Ok();
        result.StartTime = DateTime.UtcNow;

        if (missingColumns.Count == 0)
        {
            result.EndTime = DateTime.UtcNow;
            return result;
        }

        try
        {
            var connectionString = _connectionBuilder.BuildDatabaseConnectionString(_options);

            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            foreach (var column in missingColumns)
            {
                try
                {
                    var alterTableSql = BuildAddColumnSql(tableName, column);

                    _logger?.LogInformation("Adding column to table: {TableName}.{ColumnName}",
                        tableName, column.ColumnName);

                    using var command = connection.CreateCommand();
                    command.CommandText = alterTableSql;

                    await command.ExecuteNonQueryAsync(cancellationToken);

                    _logger?.LogInformation("Column added successfully: {TableName}.{ColumnName}",
                        tableName, column.ColumnName);

                    result.AddChange(new SecSchemaChange
                    {
                        TableName = tableName,
                        ColumnName = column.ColumnName,
                        ChangeType = SecSchemaChangeType.ColumnAdded,
                        Description = $"Column '{column.ColumnName}' added to table '{tableName}' with type '{column.MySQLType}'",
                    });
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error adding column: {TableName}.{ColumnName}",
                        tableName, column.ColumnName);

                    result.Success = false;
                    result.AddChange(new SecSchemaChange
                    {
                        TableName = tableName,
                        ColumnName = column.ColumnName,
                        ChangeType = SecSchemaChangeType.Error,
                        Description = $"Failed to add column '{column.ColumnName}': {ex.Message}",
                        IsSuccess = false,
                        ErrorMessage = ex.Message,
                    });
                }
            }

            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in AddMissingColumnsAsync for table: {TableName}", tableName);
            result.Success = false;
            result.Message = $"Failed to add columns: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Builds a CREATE TABLE SQL statement from table metadata.
    /// </summary>
    private static string BuildCreateTableSql(SecTableMetadata tableMetadata)
    {
        var lines = new List<string>();
        var tableName = QuoteIdentifier(tableMetadata.TableName);

        lines.Add($"CREATE TABLE IF NOT EXISTS {tableName} (");

        for (int i = 0; i < tableMetadata.Columns.Count; i++)
        {
            var column = tableMetadata.Columns[i];
            var columnDef = BuildColumnDefinition(column);
            lines.Add(columnDef + (i < tableMetadata.Columns.Count - 1 ? "," : ""));
        }

        // Add primary key constraint if present
        if (tableMetadata.PrimaryKeyColumn != null)
        {
            var pkColumnName = QuoteIdentifier(tableMetadata.PrimaryKeyColumn.ColumnName);
            lines.Add($", PRIMARY KEY ({pkColumnName})");
        }

        lines.Add(")");

        return string.Join(Environment.NewLine, lines);
    }

    /// <summary>
    /// Builds a column definition for CREATE/ALTER TABLE.
    /// </summary>
    private static string BuildColumnDefinition(SecColumnMetadata column)
    {
        var columnName = QuoteIdentifier(column.ColumnName);
        var typeDef = column.MySQLType;

        var parts = new List<string> { columnName, typeDef };

        if (!column.IsNullable)
        {
            parts.Add("NOT NULL");
        }

        if (column.IsAutoIncrement)
        {
            parts.Add("AUTO_INCREMENT");
        }

        if (!string.IsNullOrWhiteSpace(column.DefaultValue))
        {
            parts.Add($"DEFAULT {column.DefaultValue}");
        }

        return string.Join(" ", parts);
    }

    /// <summary>
    /// Builds an ALTER TABLE ADD COLUMN statement.
    /// </summary>
    private static string BuildAddColumnSql(string tableName, SecColumnMetadata column)
    {
        var table = QuoteIdentifier(tableName);
        var columnDef = BuildColumnDefinition(column);
        return $"ALTER TABLE {table} ADD COLUMN {columnDef}";
    }

    /// <summary>
    /// Safely quotes an identifier for MySQL.
    /// </summary>
    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier cannot be empty", nameof(identifier));
        }

        var escaped = identifier.Replace("`", "``");
        return $"`{escaped}`";
    }
}

/// <summary>
/// Represents metadata about an existing column in the database.
/// </summary>
public sealed class SecExistingColumn
{
    public string ColumnName { get; set; } = string.Empty;
    public string ColumnType { get; set; } = string.Empty;
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsAutoIncrement { get; set; }
}
