using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using SecNetData.Configuration;
using SecNetData.Services;

namespace SecNetData.Schema;

/// <summary>
/// Manages database-level MySQL operations (server connection, database creation, checking).
/// </summary>
public sealed class SecDatabaseManager
{
    private readonly SecDatabaseOptions _options;
    private readonly SecConnectionStringBuilder _connectionBuilder;
    private readonly ILogger<SecDatabaseManager>? _logger;

    public SecDatabaseManager(
        SecDatabaseOptions options,
        SecConnectionStringBuilder connectionBuilder,
        ILogger<SecDatabaseManager>? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _connectionBuilder = connectionBuilder ?? throw new ArgumentNullException(nameof(connectionBuilder));
        _logger = logger;
    }

    /// <summary>
    /// Checks whether the configured database exists.
    /// </summary>
    public async Task<bool> DatabaseExistsAsync(CancellationToken cancellationToken = default)
    {
        var serverConnectionString = _connectionBuilder.BuildServerConnectionString(_options);

        try
        {
            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT SCHEMA_NAME
                FROM information_schema.SCHEMATA
                WHERE SCHEMA_NAME = @databaseName
                LIMIT 1";

            command.Parameters.AddWithValue("@databaseName", _options.DatabaseName);

            var result = await command.ExecuteScalarAsync(cancellationToken);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error checking if database exists: {DatabaseName}", _options.DatabaseName);
            throw;
        }
    }

    /// <summary>
    /// Creates the configured database if it doesn't exist.
    /// </summary>
    public async Task<SecSchemaResult> CreateDatabaseIfNotExistsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = SecSchemaResult.Ok();
        result.StartTime = DateTime.UtcNow;

        try
        {
            var exists = await DatabaseExistsAsync(cancellationToken);
            if (exists)
            {
                _logger?.LogInformation("Database exists: {DatabaseName}", _options.DatabaseName);
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            _logger?.LogInformation("Creating database: {DatabaseName}", _options.DatabaseName);

            var serverConnectionString = _connectionBuilder.BuildServerConnectionString(_options);

            using var connection = new MySqlConnection(serverConnectionString);
            await connection.OpenAsync(cancellationToken);

            // Build safely quoted CREATE DATABASE statement
            var databaseName = QuoteIdentifier(_options.DatabaseName);
            var charset = QuoteIdentifier(_options.Charset);
            var collation = QuoteIdentifier(_options.Collation);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                CREATE DATABASE {databaseName}
                CHARACTER SET {charset}
                COLLATE {collation}";

            await command.ExecuteNonQueryAsync(cancellationToken);

            _logger?.LogInformation("Database created successfully: {DatabaseName}", _options.DatabaseName);

            result.AddChange(new SecSchemaChange
            {
                ChangeType = SecSchemaChangeType.DatabaseCreated,
                Description = $"Database '{_options.DatabaseName}' created with charset '{_options.Charset}' and collation '{_options.Collation}'",
            });

            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating database: {DatabaseName}", _options.DatabaseName);
            result.Success = false;
            result.Message = $"Failed to create database: {ex.Message}";
            result.AddChange(new SecSchemaChange
            {
                ChangeType = SecSchemaChangeType.Error,
                TableName = _options.DatabaseName,
                Description = $"Failed to create database: {ex.Message}",
                IsSuccess = false,
                ErrorMessage = ex.Message,
            });
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Gets information about existing tables in the database.
    /// </summary>
    public async Task<List<string>> GetExistingTablesAsync(CancellationToken cancellationToken = default)
    {
        var tables = new List<string>();
        var connectionString = _connectionBuilder.BuildDatabaseConnectionString(_options);

        try
        {
            using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = $@"
                SELECT TABLE_NAME
                FROM information_schema.TABLES
                WHERE TABLE_SCHEMA = @databaseName
                AND TABLE_TYPE = 'BASE TABLE'
                ORDER BY TABLE_NAME";

            command.Parameters.AddWithValue("@databaseName", _options.DatabaseName);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error retrieving existing tables from database: {DatabaseName}", _options.DatabaseName);
            throw;
        }
    }

    /// <summary>
    /// Safely quotes an identifier (table/column/database name) for MySQL.
    /// </summary>
    private static string QuoteIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("Identifier cannot be empty", nameof(identifier));
        }

        // MySQL uses backticks for identifier quoting
        // Escape any backticks in the identifier
        var escaped = identifier.Replace("`", "``");
        return $"`{escaped}`";
    }

    /// <summary>
    /// Validates a database identifier name.
    /// </summary>
    public static bool IsValidIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return false;

        // MySQL identifiers can be up to 64 characters
        if (identifier.Length > 64)
            return false;

        // Must start with letter, digit, or underscore
        var firstChar = identifier[0];
        if (!char.IsLetterOrDigit(firstChar) && firstChar != '_')
            return false;

        // Remaining characters can be letters, digits, underscores, or $ (in some contexts)
        for (int i = 1; i < identifier.Length; i++)
        {
            var ch = identifier[i];
            if (!char.IsLetterOrDigit(ch) && ch != '_' && ch != '$')
                return false;
        }

        return true;
    }
}
