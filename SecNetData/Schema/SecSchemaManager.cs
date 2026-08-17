using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SecNetData.Configuration;
using SecNetData.Services;

namespace SecNetData.Schema;

/// <summary>
/// Orchestrates complete schema synchronization between entity models and MySQL database.
/// </summary>
public sealed class SecSchemaManager : ISecSchemaManager
{
    private readonly SecEntityDiscovery _entityDiscovery;
    private readonly MySQLTypeMapper _typeMapper;
    private readonly SecDatabaseManager _databaseManager;
    private readonly SecTableManager _tableManager;
    private readonly SecSchemaComparator _comparator;
    private readonly SecSchemaOptions _options;
    private readonly ILogger<SecSchemaManager>? _logger;

    public SecSchemaManager(
        SecEntityDiscovery entityDiscovery,
        SecDatabaseManager databaseManager,
        SecTableManager tableManager,
        SecSchemaOptions options,
        ILogger<SecSchemaManager>? logger = null)
    {
        _entityDiscovery = entityDiscovery ?? throw new ArgumentNullException(nameof(entityDiscovery));
        _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        _tableManager = tableManager ?? throw new ArgumentNullException(nameof(tableManager));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        _typeMapper = new MySQLTypeMapper();
        _comparator = new SecSchemaComparator(null);
    }

    /// <summary>
    /// Synchronizes the database schema with the application's SecEntity models.
    /// </summary>
    public async Task<SecSchemaResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        var overallResult = SecSchemaResult.Ok("Schema synchronization started.");
        overallResult.StartTime = DateTime.UtcNow;

        try
        {
            _logger?.LogInformation("Starting schema synchronization");

            // Step 1: Create database if needed
            if (_options.AutoCreateDatabase)
            {
                _logger?.LogInformation("Checking/creating database: {DatabaseName}",
                    _options.DatabaseName);

                var dbResult = await _databaseManager.CreateDatabaseIfNotExistsAsync(cancellationToken);
                overallResult.Changes.AddRange(dbResult.Changes);

                if (!dbResult.Success)
                {
                    overallResult.Success = false;
                    overallResult.Message = "Failed to create database";
                    overallResult.EndTime = DateTime.UtcNow;
                    return overallResult;
                }
            }

            // Step 2: Discover entities
            var entityTypes = _entityDiscovery.GetEntities();
            _logger?.LogInformation("Discovered {EntityCount} SecEntity types", entityTypes.Count);

            if (entityTypes.Count == 0)
            {
                _logger?.LogWarning("No SecEntity types discovered");
                overallResult.EndTime = DateTime.UtcNow;
                return overallResult;
            }

            // Step 3: Get existing tables
            var existingTables = await _databaseManager.GetExistingTablesAsync(cancellationToken);
            _logger?.LogDebug("Found {TableCount} existing tables in database", existingTables.Count);

            // Step 4: Synchronize each entity/table
            foreach (var entityType in entityTypes)
            {
                var entityResult = await SynchronizeEntityAsync(
                    entityType,
                    existingTables,
                    cancellationToken);

                overallResult.Changes.AddRange(entityResult.Changes);

                if (!entityResult.Success)
                {
                    overallResult.Success = false;
                }
            }

            _logger?.LogInformation("Schema synchronization completed. Success: {Success}",
                overallResult.Success);

            overallResult.Message = overallResult.Success
                ? "Schema synchronization completed successfully"
                : "Schema synchronization completed with errors";

            overallResult.EndTime = DateTime.UtcNow;
            return overallResult;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Schema synchronization failed");
            overallResult.Success = false;
            overallResult.Message = $"Schema synchronization failed: {ex.Message}";
            overallResult.AddChange(new SecSchemaChange
            {
                ChangeType = SecSchemaChangeType.Error,
                Description = $"Schema synchronization failed: {ex.Message}",
                IsSuccess = false,
                ErrorMessage = ex.Message,
            });
            overallResult.EndTime = DateTime.UtcNow;
            return overallResult;
        }
    }

    /// <summary>
    /// Checks whether the schema is synchronized without making changes.
    /// </summary>
    public async Task<SecSchemaResult> CheckSchemaAsync(CancellationToken cancellationToken = default)
    {
        var result = SecSchemaResult.Ok("Schema check completed.");
        result.StartTime = DateTime.UtcNow;

        try
        {
            _logger?.LogInformation("Starting schema check");

            var entityTypes = _entityDiscovery.GetEntities();
            _logger?.LogInformation("Checking {EntityCount} SecEntity types", entityTypes.Count);

            if (entityTypes.Count == 0)
            {
                result.EndTime = DateTime.UtcNow;
                return result;
            }

            var existingTables = await _databaseManager.GetExistingTablesAsync(cancellationToken);

            foreach (var entityType in entityTypes)
            {
                var entityMetadata = SecEntityMetadata.FromEntityType(entityType);
                var tableMetadata = SecTableMetadata.FromEntityMetadata(entityMetadata, _typeMapper);

                var tableExists = existingTables.Contains(tableMetadata.TableName);
                if (!tableExists)
                {
                    result.AddChange(new SecSchemaChange
                    {
                        TableName = tableMetadata.TableName,
                        ChangeType = SecSchemaChangeType.Warning,
                        Description = $"Table '{tableMetadata.TableName}' does not exist",
                    });
                    continue;
                }

                var existingColumns = await _tableManager.GetExistingColumnsAsync(
                    tableMetadata.TableName,
                    cancellationToken);

                var comparison = _comparator.CompareTableSchema(tableMetadata, existingColumns);

                if (!comparison.IsSynchronized)
                {
                    foreach (var missing in comparison.MissingColumns)
                    {
                        result.AddChange(new SecSchemaChange
                        {
                            TableName = tableMetadata.TableName,
                            ColumnName = missing.ColumnName,
                            ChangeType = SecSchemaChangeType.Warning,
                            Description = $"Column '{missing.ColumnName}' is missing from table '{tableMetadata.TableName}'",
                        });
                    }

                    foreach (var diff in comparison.ColumnDifferences)
                    {
                        result.AddChange(new SecSchemaChange
                        {
                            TableName = tableMetadata.TableName,
                            ColumnName = diff.ColumnName,
                            ChangeType = SecSchemaChangeType.UnsupportedChange,
                            Description = $"Column definition mismatch in '{tableMetadata.TableName}.{diff.ColumnName}': {string.Join("; ", diff.Issues)}",
                        });
                    }
                }
            }

            result.EndTime = DateTime.UtcNow;
            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Schema check failed");
            result.Success = false;
            result.Message = $"Schema check failed: {ex.Message}";
            result.EndTime = DateTime.UtcNow;
            return result;
        }
    }

    /// <summary>
    /// Synchronizes a single entity's schema.
    /// </summary>
    private async Task<SecSchemaResult> SynchronizeEntityAsync(
        Type entityType,
        List<string> existingTables,
        CancellationToken cancellationToken)
    {
        var result = SecSchemaResult.Ok();

        try
        {
            // Extract entity metadata
            var entityMetadata = SecEntityMetadata.FromEntityType(entityType);
            var tableMetadata = SecTableMetadata.FromEntityMetadata(entityMetadata, _typeMapper);

            // Validate table metadata
            var validationErrors = tableMetadata.Validate();
            if (validationErrors.Count > 0)
            {
                result.Success = false;
                foreach (var error in validationErrors)
                {
                    result.AddChange(new SecSchemaChange
                    {
                        EntityName = entityType.Name,
                        TableName = tableMetadata.TableName,
                        ChangeType = SecSchemaChangeType.Error,
                        Description = error,
                        IsSuccess = false,
                    });
                }
                return result;
            }

            var tableExists = existingTables.Contains(tableMetadata.TableName);

            if (!tableExists)
            {
                if (!_options.AutoCreateTables)
                {
                    _logger?.LogWarning("Table '{TableName}' does not exist and auto-create is disabled",
                        tableMetadata.TableName);
                    result.AddChange(new SecSchemaChange
                    {
                        TableName = tableMetadata.TableName,
                        ChangeType = SecSchemaChangeType.Warning,
                        Description = $"Table '{tableMetadata.TableName}' does not exist and auto-create is disabled",
                    });
                    return result;
                }

                // Create the table
                var createResult = await _tableManager.CreateTableAsync(tableMetadata, cancellationToken);
                result.Changes.AddRange(createResult.Changes);
                result.Success = result.Success && createResult.Success;
                return result;
            }

            // Table exists - check for missing columns
            if (_options.AutoAddColumns)
            {
                var existingColumns = await _tableManager.GetExistingColumnsAsync(
                    tableMetadata.TableName,
                    cancellationToken);

                var comparison = _comparator.CompareTableSchema(tableMetadata, existingColumns);

                if (comparison.MissingColumns.Count > 0)
                {
                    var addResult = await _tableManager.AddMissingColumnsAsync(
                        tableMetadata.TableName,
                        comparison.MissingColumns,
                        cancellationToken);

                    result.Changes.AddRange(addResult.Changes);
                    result.Success = result.Success && addResult.Success;
                }

                // Report on column differences (but don't fix them)
                if (comparison.ColumnDifferences.Count > 0)
                {
                    foreach (var diff in comparison.ColumnDifferences)
                    {
                        result.AddChange(new SecSchemaChange
                        {
                            TableName = tableMetadata.TableName,
                            ColumnName = diff.ColumnName,
                            ChangeType = SecSchemaChangeType.UnsupportedChange,
                            Description = $"Column definition mismatch in '{tableMetadata.TableName}.{diff.ColumnName}': {string.Join("; ", diff.Issues)}",
                        });
                    }
                }

                // Report extra columns in database
                if (comparison.ExtraColumns.Count > 0)
                {
                    foreach (var extra in comparison.ExtraColumns)
                    {
                        _logger?.LogWarning("Extra column in table {TableName}: {ColumnName}",
                            tableMetadata.TableName, extra.ColumnName);

                        result.AddChange(new SecSchemaChange
                        {
                            TableName = tableMetadata.TableName,
                            ColumnName = extra.ColumnName,
                            ChangeType = SecSchemaChangeType.Warning,
                            Description = $"Extra column '{extra.ColumnName}' exists in table '{tableMetadata.TableName}' but is not defined in the model",
                        });
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error synchronizing entity: {EntityType}", entityType.Name);
            result.Success = false;
            result.AddChange(new SecSchemaChange
            {
                EntityName = entityType.Name,
                ChangeType = SecSchemaChangeType.Error,
                Description = $"Failed to synchronize entity '{entityType.Name}': {ex.Message}",
                IsSuccess = false,
                ErrorMessage = ex.Message,
            });
            return result;
        }
    }
}
