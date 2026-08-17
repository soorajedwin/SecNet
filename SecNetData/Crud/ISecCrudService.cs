using System;
using System.Threading;
using System.Threading.Tasks;
using SecNetCore.Models;
using SecNetCore.Results;

namespace SecNetData.Crud;

/// <summary>
/// Generic CRUD service for all SecEntity types.
/// Operates dynamically on any registered [SecEntity] without entity-specific code.
/// </summary>
public interface ISecCrudService
{
    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <param name="entityName">The entity type name (e.g., "Customer").</param>
    /// <param name="model">The model data to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the created entity or failure details.</returns>
    Task<SecResult<object?>> CreateAsync(
        string entityName,
        object model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an entity by its primary key.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the entity or null if not found.</returns>
    Task<SecResult<object?>> GetByIdAsync(
        string entityName,
        object id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a paged list of entities with optional searching and sorting.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="request">Query parameters (paging, sorting, searching).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing paged list of entities.</returns>
    Task<SecResult<SecPagedResult<object>>> GetListAsync(
        string entityName,
        SecQueryRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="id">The primary key of the entity to update.</param>
    /// <param name="model">The model data with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the updated entity or failure details.</returns>
    Task<SecResult<object?>> UpdateAsync(
        string entityName,
        object id,
        object model,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entity.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="id">The primary key of the entity to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure.</returns>
    Task<SecResult<bool>> DeleteAsync(
        string entityName,
        object id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether an entity exists by primary key.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing true if exists, false otherwise.</returns>
    Task<SecResult<bool>> ExistsAsync(
        string entityName,
        object id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts all entities of a given type.
    /// </summary>
    /// <param name="entityName">The entity type name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the entity count.</returns>
    Task<SecResult<int>> CountAsync(
        string entityName,
        CancellationToken cancellationToken = default);
}
