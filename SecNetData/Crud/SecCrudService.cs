using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecNetCore.Models;
using SecNetCore.Results;
using SecNetData.Configuration;
using SecNetData.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SecNetData.Crud;

/// <summary>
/// Generic CRUD service that operates on any [SecEntity] without entity-specific code.
/// </summary>
public sealed class SecCrudService : ISecCrudService
{
    private readonly SecDbContext _dbContext;
    private readonly ISecEntityRegistry _entityRegistry;
    private readonly ISecValidationService _validationService;
    private readonly ISecTypeConverter _typeConverter;
    private readonly ISecPropertyMapper _propertyMapper;
    private readonly ISecSortingExpressionBuilder _sortingBuilder;
    private readonly ISecSearchExpressionBuilder _searchBuilder;
    private readonly SecCrudOptions _crudOptions;
    private readonly ILogger<SecCrudService> _logger;

    public SecCrudService(
        SecDbContext dbContext,
        ISecEntityRegistry entityRegistry,
        ISecValidationService validationService,
        ISecTypeConverter typeConverter,
        ISecPropertyMapper propertyMapper,
        ISecSortingExpressionBuilder sortingBuilder,
        ISecSearchExpressionBuilder searchBuilder,
        SecCrudOptions crudOptions,
        ILogger<SecCrudService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _entityRegistry = entityRegistry ?? throw new ArgumentNullException(nameof(entityRegistry));
        _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
        _typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
        _propertyMapper = propertyMapper ?? throw new ArgumentNullException(nameof(propertyMapper));
        _sortingBuilder = sortingBuilder ?? throw new ArgumentNullException(nameof(sortingBuilder));
        _searchBuilder = searchBuilder ?? throw new ArgumentNullException(nameof(searchBuilder));
        _crudOptions = crudOptions ?? throw new ArgumentNullException(nameof(crudOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SecResult<object?>> CreateAsync(
        string entityName,
        object model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Creating entity: {EntityName}", entityName);

            // Resolve entity type
            if (!_entityRegistry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            {
                _logger.LogWarning("Unknown entity: {EntityName}", entityName);
                return SecResult<object?>.Fail($"Unknown entity: '{entityName}'");
            }

            // Check Create permission
            if (!_entityRegistry.IsOperationAllowed(entityType, SecCrud.Create))
            {
                _logger.LogWarning("Create not allowed for entity: {EntityName}", entityName);
                return SecResult<object?>.Fail($"Create operation not allowed for entity '{entityName}'");
            }

            // Create instance of entity
            var entity = Activator.CreateInstance(entityType);
            if (entity == null)
            {
                _logger.LogError("Failed to create instance of {EntityType}", entityType.Name);
                return SecResult<object?>.Fail($"Failed to create entity instance");
            }

            _logger.LogInformation(
    "Incoming model Name: {Name}",
    entityType.GetProperty("Name")?.GetValue(model));

            _logger.LogInformation(
                "Incoming model Email: {Email}",
                entityType.GetProperty("Email")?.GetValue(model));

            _logger.LogInformation(
                "Incoming model IsActive: {IsActive}",
                entityType.GetProperty("IsActive")?.GetValue(model));

            // DEBUG: inspect the actual incoming model
            _logger.LogInformation(
                "Create model type: {ModelType}",
                model?.GetType().FullName);

            if (model is JsonElement jsonElement)
            {
                _logger.LogInformation(
                    "Create JSON: {Json}",
                    jsonElement.GetRawText());
            }

            _logger.LogInformation("BEFORE MapProperties");

            var mappingSuccess = _propertyMapper.MapProperties(
                model,
                entity,
                _entityRegistry,
                _typeConverter,
                out var mapErrors);

            _logger.LogInformation(
                "AFTER MapProperties. Success={Success}, Errors={Errors}",
                mappingSuccess,
                string.Join("; ", mapErrors));

            _logger.LogInformation(
                "Entity Name AFTER mapping: {Name}",
                entityType.GetProperty("Name")?.GetValue(entity));

            _logger.LogInformation(
                "Entity Email AFTER mapping: {Email}",
                entityType.GetProperty("Email")?.GetValue(entity));

            _logger.LogInformation(
                "Entity IsActive AFTER mapping: {IsActive}",
                entityType.GetProperty("IsActive")?.GetValue(entity));

            if (!mappingSuccess)
            {
                _logger.LogWarning(
                    "Property mapping failed for {EntityName}: {Errors}",
                    entityName,
                    string.Join("; ", mapErrors));

                var result = SecResult<object?>.Fail("Property mapping failed");
                result.Errors = mapErrors;
                return result;
            }

            //// Map properties from model to entity
            //if (!_propertyMapper.MapProperties(model, entity, _entityRegistry, _typeConverter, out var mapErrors))
            //{
            //    _logger.LogWarning("Property mapping failed for {EntityName}: {Errors}", entityName,
            //        string.Join("; ", mapErrors));
            //    var result = SecResult<object?>.Fail("Property mapping failed");
            //    result.Errors = mapErrors;
            //    return result;
            //}

            // Validate the entity after mapping
            _logger.LogInformation("BEFORE Validate");
            if (!_validationService.Validate(entity, out var entityErrors))
            {
                _logger.LogWarning("Entity validation failed for {EntityName}", entityName);
                var result = SecResult<object?>.Fail("Entity validation failed");
                result.Errors = entityErrors;
                return result;
            }
            _logger.LogInformation("AFTER Validate");
            // Add to DbContext
            _dbContext.Add(entity);

            // Save changes
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Entity created successfully: {EntityName}", entityName);

            return SecResult<object?>.Ok(entity, "Entity created successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating entity: {EntityName}", entityName);
            return SecResult<object?>.Fail($"Failed to create entity: {ex.Message}");
        }
    }

    public async Task<SecResult<object?>> GetByIdAsync(
        string entityName,
        object id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting entity by ID: {EntityName}, ID: {Id}", entityName, id);

            // Resolve entity type
            if (!_entityRegistry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            {
                return SecResult<object?>.Fail($"Unknown entity: '{entityName}'");
            }

            // Check Read permission
            if (!_entityRegistry.IsOperationAllowed(entityType, SecCrud.Read))
            {
                return SecResult<object?>.Fail($"Read operation not allowed for entity '{entityName}'");
            }

            // Get primary key metadata and convert ID
            var pkProperty = _entityRegistry.GetPrimaryKeyProperty(entityType);
            if (!_typeConverter.TryConvert(id, pkProperty.PropertyType, out var convertedId, out var conversionError))
            {
                _logger.LogWarning("Invalid ID for {EntityName}: {Error}", entityName, conversionError);
                return SecResult<object?>.Fail($"Invalid ID: {conversionError}");
            }

            // Build query for the entity set using reflection
            var entitySetMethod = typeof(DbContext)
                .GetMethods()
                .First(m => m.Name == "Set" && m.GetGenericArguments().Length == 1);
            var genericMethod = entitySetMethod.MakeGenericMethod(entityType);
            var entitySet = (IQueryable)genericMethod.Invoke(_dbContext, null)!;
            var query = ApplyAsNoTracking(entitySet, entityType);

            // Find by primary key
            var entity = await GetEntityByKeyAsync(query, entityType, pkProperty.PropertyName, convertedId, cancellationToken);

            if (entity == null)
            {
                _logger.LogInformation("Entity not found: {EntityName}, ID: {Id}", entityName, id);
                return SecResult<object?>.Fail($"Entity not found");
            }

            return SecResult<object?>.Ok(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity: {EntityName}", entityName);
            return SecResult<object?>.Fail($"Failed to retrieve entity: {ex.Message}");
        }
    }

    public async Task<SecResult<SecPagedResult<object>>> GetListAsync(
        string entityName,
        SecQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Getting list for entity: {EntityName}", entityName);

            // Resolve entity type
            if (!_entityRegistry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            {
                return SecResult<SecPagedResult<object>>.Fail($"Unknown entity: '{entityName}'");
            }

            // Check Read permission
            if (!_entityRegistry.IsOperationAllowed(entityType, SecCrud.Read))
            {
                return SecResult<SecPagedResult<object>>.Fail(
                    $"Read operation not allowed for entity '{entityName}'");
            }

            // Validate pagination
            request ??= new SecQueryRequest();
            request.Page ??= new SecPageRequest();
            request.Page.Page = request.Page.Page < 1 ? 1 : request.Page.Page;
            request.Page.PageSize = request.Page.PageSize <= 0 ? _crudOptions.DefaultPageSize : request.Page.PageSize;
            request.Page.PageSize = Math.Min(request.Page.PageSize, _crudOptions.MaxPageSize);

            // Get query using reflection for generic Set<T>
            var entitySetMethod = typeof(DbContext)
                .GetMethods()
                .First(m => m.Name == "Set" && m.GetGenericArguments().Length == 1);
            var genericMethod = entitySetMethod.MakeGenericMethod(entityType);
            var entitySet = (IQueryable)genericMethod.Invoke(_dbContext, null)!;
            var query = ApplyAsNoTracking(entitySet, entityType);

            // Apply search
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                query = (IQueryable)_searchBuilder.ApplySearch(query, request.Search, entityType, _entityRegistry);
            }

            // Apply sorting
            query = (IQueryable)_sortingBuilder.ApplySorting(query, request.Sorts, entityType, _entityRegistry, out var sortErrors);
            if (sortErrors.Count > 0)
            {
                _logger.LogWarning("Sort errors for {EntityName}: {Errors}", entityName,
                    string.Join("; ", sortErrors));
            }

            // Get total count using reflection
            var countAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .FirstOrDefault(m => m.Name == "CountAsync" && m.GetParameters().Length == 2)
                ?? typeof(Queryable)
                    .GetMethods()
                    .First(m => m.Name == "CountAsync" && m.GetParameters().Length == 2);

            countAsyncMethod = countAsyncMethod.MakeGenericMethod(entityType);
            var countTask = (Task<int>)countAsyncMethod.Invoke(null, new object[] { query, cancellationToken })!;
            var totalCount = await countTask;

            // Apply paging using reflection
            var skip = (request.Page.Page - 1) * request.Page.PageSize;

            var skipMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == "Skip" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);
            query = (IQueryable)skipMethod.Invoke(null, new object[] { query, skip })!;

            var takeMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == "Take" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);
            query = (IQueryable)takeMethod.Invoke(null, new object[] { query, request.Page.PageSize })!;

            var toListAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m => m.Name == "ToListAsync" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);

            var itemsTask = (Task)toListAsyncMethod.Invoke(
                null,
                new object[] { query, cancellationToken })!;

            await itemsTask;

            var resultProperty = itemsTask.GetType().GetProperty("Result");

            if (resultProperty == null)
            {
                throw new InvalidOperationException(
                    $"Unable to retrieve query result for entity '{entityName}'.");
            }

            var result = resultProperty.GetValue(itemsTask);

            var items = result is System.Collections.IEnumerable enumerable
                ? enumerable.Cast<object>().ToList()
                : new List<object>();

            var pagedResult = new SecPagedResult<object>
            {
                Items = items,
                Page = request.Page.Page,
                PageSize = request.Page.PageSize,
                TotalRecords = totalCount,
            };

            return SecResult<SecPagedResult<object>>.Ok(pagedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting list for entity: {EntityName}", entityName);
            return SecResult<SecPagedResult<object>>.Fail($"Failed to retrieve list: {ex.Message}");
        }
    }

    public async Task<SecResult<object?>> UpdateAsync(
        string entityName,
        object id,
        object model,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Updating entity: {EntityName}, ID: {Id}", entityName, id);

            // Resolve entity type
            if (!_entityRegistry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            {
                return SecResult<object?>.Fail($"Unknown entity: '{entityName}'");
            }

            // Check Update permission
            if (!_entityRegistry.IsOperationAllowed(entityType, SecCrud.Update))
            {
                return SecResult<object?>.Fail($"Update operation not allowed for entity '{entityName}'");
            }

            // Get primary key metadata and convert ID
            var pkProperty = _entityRegistry.GetPrimaryKeyProperty(entityType);
            if (!_typeConverter.TryConvert(id, pkProperty.PropertyType, out var convertedId, out var conversionError))
            {
                _logger.LogWarning("Invalid ID for {EntityName}: {Error}", entityName, conversionError);
                return SecResult<object?>.Fail($"Invalid ID: {conversionError}");
            }

            // Find existing entity using reflection
            var entitySetMethod = typeof(DbContext)
                .GetMethods()
                .First(m => m.Name == "Set" && m.GetGenericArguments().Length == 1);
            var genericMethod = entitySetMethod.MakeGenericMethod(entityType);
            var entitySet = (IQueryable)genericMethod.Invoke(_dbContext, null)!;
            var entity = await GetEntityByKeyAsync(entitySet, entityType, pkProperty.PropertyName, convertedId, cancellationToken);

            if (entity == null)
            {
                _logger.LogInformation("Entity not found for update: {EntityName}, ID: {Id}", entityName, id);
                return SecResult<object?>.Fail($"Entity not found");
            }

            // Validate input model
            if (!_validationService.Validate(model, out var validationErrors))
            {
                _logger.LogWarning("Validation failed for update: {EntityName}", entityName);
                var result = SecResult<object?>.Fail("Validation failed");
                result.Errors = validationErrors;
                return result;
            }

            // Map properties from model to entity (excludes PK automatically)
            if (!_propertyMapper.MapProperties(model, entity, _entityRegistry, _typeConverter, out var mapErrors))
            {
                _logger.LogWarning("Property mapping failed for update: {EntityName}: {Errors}", entityName,
                    string.Join("; ", mapErrors));
                var result = SecResult<object?>.Fail("Property mapping failed");
                result.Errors = mapErrors;
                return result;
            }

            // Validate the entity after mapping
            if (!_validationService.Validate(entity, out var entityErrors))
            {
                _logger.LogWarning("Entity validation failed for update: {EntityName}", entityName);
                var result = SecResult<object?>.Fail("Entity validation failed");
                result.Errors = entityErrors;
                return result;
            }

            // Save changes
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Entity updated successfully: {EntityName}, ID: {Id}", entityName, id);

            return SecResult<object?>.Ok(entity, "Entity updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity: {EntityName}", entityName);
            return SecResult<object?>.Fail($"Failed to update entity: {ex.Message}");
        }
    }

    public async Task<SecResult<bool>> DeleteAsync(
        string entityName,
        object id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Deleting entity: {EntityName}, ID: {Id}", entityName, id);

            // Resolve entity type
            if (!_entityRegistry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            {
                return SecResult<bool>.Fail($"Unknown entity: '{entityName}'");
            }

            // Check Delete permission
            if (!_entityRegistry.IsOperationAllowed(entityType, SecCrud.Delete))
            {
                return SecResult<bool>.Fail($"Delete operation not allowed for entity '{entityName}'");
            }

            // Get primary key metadata and convert ID
            var pkProperty = _entityRegistry.GetPrimaryKeyProperty(entityType);
            if (!_typeConverter.TryConvert(id, pkProperty.PropertyType, out var convertedId, out var conversionError))
            {
                _logger.LogWarning("Invalid ID for {EntityName}: {Error}", entityName, conversionError);
                return SecResult<bool>.Fail($"Invalid ID: {conversionError}");
            }

            // Find existing entity using reflection
            var entitySetMethod = typeof(DbContext)
                .GetMethods()
                .First(m => m.Name == "Set" && m.GetGenericArguments().Length == 1);
            var genericMethod = entitySetMethod.MakeGenericMethod(entityType);
            var entitySet = (IQueryable)genericMethod.Invoke(_dbContext, null)!;
            var entity = await GetEntityByKeyAsync(entitySet, entityType, pkProperty.PropertyName, convertedId, cancellationToken);

            if (entity == null)
            {
                _logger.LogInformation("Entity not found for delete: {EntityName}, ID: {Id}", entityName, id);
                return SecResult<bool>.Fail($"Entity not found");
            }

            // Delete
            _dbContext.Remove(entity);
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Entity deleted successfully: {EntityName}, ID: {Id}", entityName, id);

            return SecResult<bool>.Ok(true, "Entity deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity: {EntityName}", entityName);
            return SecResult<bool>.Fail($"Failed to delete entity: {ex.Message}");
        }
    }

    public async Task<SecResult<bool>> ExistsAsync(
        string entityName,
        object id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking existence: {EntityName}, ID: {Id}", entityName, id);

            // Resolve entity type
            if (!_entityRegistry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            {
                return SecResult<bool>.Fail($"Unknown entity: '{entityName}'");
            }

            // Get primary key metadata and convert ID
            var pkProperty = _entityRegistry.GetPrimaryKeyProperty(entityType);
            if (!_typeConverter.TryConvert(id, pkProperty.PropertyType, out var convertedId, out var conversionError))
            {
                return SecResult<bool>.Fail($"Invalid ID: {conversionError}");
            }

            // Check existence efficiently using reflection
            var entitySetMethod = typeof(DbContext)
                .GetMethods()
                .First(m => m.Name == "Set" && m.GetGenericArguments().Length == 1);
            var genericMethod = entitySetMethod.MakeGenericMethod(entityType);
            var entitySet = (IQueryable)genericMethod.Invoke(_dbContext, null)!;
            var query = ApplyAsNoTracking(entitySet, entityType);
            var exists = await EntityExistsAsync(query, entityType, pkProperty.PropertyName, convertedId, cancellationToken);

            return SecResult<bool>.Ok(exists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence: {EntityName}", entityName);
            return SecResult<bool>.Fail($"Failed to check existence: {ex.Message}");
        }
    }

    public async Task<SecResult<int>> CountAsync(
        string entityName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Counting entities: {EntityName}", entityName);

            // Resolve entity type
            if (!_entityRegistry.TryGetEntityType(entityName, out var entityType) || entityType == null)
            {
                return SecResult<int>.Fail($"Unknown entity: '{entityName}'");
            }

            // Check Read permission
            if (!_entityRegistry.IsOperationAllowed(entityType, SecCrud.Read))
            {
                return SecResult<int>.Fail($"Read operation not allowed for entity '{entityName}'");
            }

            // Count using reflection
            var entitySetMethod = typeof(DbContext)
                .GetMethods()
                .First(m => m.Name == "Set" && m.GetGenericArguments().Length == 1);
            var genericMethod = entitySetMethod.MakeGenericMethod(entityType);
            var entitySet = (IQueryable)genericMethod.Invoke(_dbContext, null)!;

            // Use reflection to call CountAsync
            var countAsyncMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .FirstOrDefault(m => m.Name == "CountAsync" && m.GetParameters().Length == 2)
                ?? typeof(Queryable)
                    .GetMethods()
                    .First(m => m.Name == "CountAsync" && m.GetParameters().Length == 2);

            countAsyncMethod = countAsyncMethod.MakeGenericMethod(entityType);
            var countTask = (Task<int>)countAsyncMethod.Invoke(null, new object[] { entitySet, cancellationToken })!;
            var count = await countTask;
            return SecResult<int>.Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities: {EntityName}", entityName);
            return SecResult<int>.Fail($"Failed to count entities: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to apply AsNoTracking to an untyped IQueryable.
    /// </summary>
    private IQueryable ApplyAsNoTracking(IQueryable query, Type entityType)
    {
        try
        {
            var asNoTrackingMethod = typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m => m.Name == "AsNoTracking" && m.GetGenericArguments().Length == 1)
                .MakeGenericMethod(entityType);

            return (IQueryable)asNoTrackingMethod.Invoke(null, new object[] { query })!;
        }
        catch
        {
            return query; // Fall back to original if AsNoTracking fails
        }
    }

    /// <summary>
    /// Helper method to get an entity by primary key using reflection.
    /// </summary>

    private async Task<object?> GetEntityByKeyAsync(
    IQueryable query,
    Type entityType,
    string keyPropertyName,
    object keyValue,
    CancellationToken cancellationToken)
    {
        // Create x => x.Key == keyValue
        var parameter =
            System.Linq.Expressions.Expression.Parameter(
                entityType,
                "x");

        var property =
            System.Linq.Expressions.Expression.Property(
                parameter,
                keyPropertyName);

        var constant =
            System.Linq.Expressions.Expression.Constant(
                keyValue,
                property.Type);

        var equality =
            System.Linq.Expressions.Expression.Equal(
                property,
                constant);

        var lambda =
            System.Linq.Expressions.Expression.Lambda(
                equality,
                parameter);

        // IQueryable<T>.Where(...)
        var whereMethod = typeof(Queryable)
            .GetMethods()
            .First(m =>
                m.Name == "Where" &&
                m.IsGenericMethod &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters().Length == 2 &&
                m.GetParameters()[1].ParameterType
                    .GetGenericTypeDefinition() == typeof(System.Linq.Expressions.Expression<>));

        var filteredQuery =
            (IQueryable)whereMethod
                .MakeGenericMethod(entityType)
                .Invoke(
                    null,
                    new object[] { query, lambda })!;

        // EntityFrameworkQueryableExtensions.FirstOrDefaultAsync<T>()
        var firstOrDefaultMethod =
            typeof(EntityFrameworkQueryableExtensions)
                .GetMethods()
                .First(m =>
                    m.Name == "FirstOrDefaultAsync" &&
                    m.IsGenericMethod &&
                    m.GetGenericArguments().Length == 1 &&
                    m.GetParameters().Length == 2 &&
                    m.GetParameters()[1].ParameterType ==
                        typeof(CancellationToken));

        var resultTask =
            (Task)firstOrDefaultMethod
                .MakeGenericMethod(entityType)
                .Invoke(
                    null,
                    new object[]
                    {
                    filteredQuery,
                    cancellationToken
                    })!;

        await resultTask;

        var resultProperty =
            resultTask.GetType().GetProperty("Result");

        return resultProperty?.GetValue(resultTask);
    }

    //private async Task<object?> GetEntityByKeyAsync(
    //    IQueryable query,
    //    Type entityType,
    //    string keyPropertyName,
    //    object keyValue,
    //    CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        // Use reflection to create a where clause for the key property
    //        var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "x");
    //        var property = System.Linq.Expressions.Expression.Property(parameter, keyPropertyName);
    //        var constant = System.Linq.Expressions.Expression.Constant(keyValue);
    //        var equality = System.Linq.Expressions.Expression.Equal(property, constant);
    //        var lambda = System.Linq.Expressions.Expression.Lambda(equality, parameter);

    //        // Apply the filter
    //        var whereMethod = typeof(Queryable)
    //            .GetMethods()
    //            .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
    //            .MakeGenericMethod(entityType);

    //        var filteredQuery = (IQueryable)whereMethod.Invoke(null, new object[] { query, lambda })!;

    //        // Get first or default
    //        var firstOrDefaultMethod = typeof(Queryable)
    //            .GetMethods()
    //            .First(m => m.Name == "FirstOrDefaultAsync" && m.GetParameters().Length == 2)
    //            .MakeGenericMethod(entityType);

    //        var resultTask = (Task)firstOrDefaultMethod.Invoke(null, new object[] { filteredQuery, cancellationToken })!;

    //        await resultTask;

    //        var resultProperty = resultTask.GetType().GetProperty("Result");

    //        return resultProperty?.GetValue(resultTask);
    //    }
    //    catch
    //    {
    //        return null;
    //    }
    //}

    /// <summary>
    /// Helper method to check entity existence by primary key.
    /// </summary>
    private async Task<bool> EntityExistsAsync(
        IQueryable query,
        Type entityType,
        string keyPropertyName,
        object keyValue,
        CancellationToken cancellationToken)
    {
        try
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(entityType, "x");
            var property = System.Linq.Expressions.Expression.Property(parameter, keyPropertyName);
            var constant = System.Linq.Expressions.Expression.Constant(keyValue);
            var equality = System.Linq.Expressions.Expression.Equal(property, constant);
            var lambda = System.Linq.Expressions.Expression.Lambda(equality, parameter);

            var whereMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);

            var filteredQuery = (IQueryable)whereMethod.Invoke(null, new object[] { query, lambda })!;

            var anyMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == "AnyAsync" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);

            var result = await (Task<bool>)anyMethod.Invoke(null, new object[] { filteredQuery, cancellationToken })!;
            return result;
        }
        catch
        {
            return false;
        }
    }
}
