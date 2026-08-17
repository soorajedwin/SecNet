using System;
using SecNetCore.Attributes;
using SecNetCore.Models;
using SecNetData.Schema;

namespace SecNetData.Crud;

/// <summary>
/// Central registry for discovering and resolving SecEntity types and their metadata.
/// </summary>
public interface ISecEntityRegistry
{
    /// <summary>
    /// Gets all registered entity types.
    /// </summary>
    IReadOnlyList<Type> GetAllEntityTypes();

    /// <summary>
    /// Attempts to resolve an entity type by entity name (e.g., "Customer").
    /// Entity name is typically the CLR class name.
    /// </summary>
    bool TryGetEntityType(string entityName, out Type? entityType);

    /// <summary>
    /// Gets the metadata for an entity type.
    /// </summary>
    SecEntityMetadata GetEntityMetadata(Type entityType);

    /// <summary>
    /// Gets the metadata for an entity by entity name.
    /// </summary>
    SecEntityMetadata GetEntityMetadata(string entityName);

    /// <summary>
    /// Gets the primary key property metadata for an entity type.
    /// </summary>
    SecPropertyMetadata GetPrimaryKeyProperty(Type entityType);

    /// <summary>
    /// Determines whether a CRUD operation is allowed for an entity.
    /// </summary>
    bool IsOperationAllowed(Type entityType, SecCrud operation);

    /// <summary>
    /// Gets the entity's SecEntity attribute.
    /// </summary>
    SecEntityAttribute GetEntityAttribute(Type entityType);
}
