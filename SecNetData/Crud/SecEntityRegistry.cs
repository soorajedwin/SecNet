using System;
using System.Collections.Generic;
using System.Linq;
using SecNetCore.Attributes;
using SecNetCore.Models;
using SecNetData.Schema;
using SecNetData.Services;

namespace SecNetData.Crud;

/// <summary>
/// Implementation of ISecEntityRegistry that caches entity metadata for performance.
/// </summary>
public sealed class SecEntityRegistry : ISecEntityRegistry
{
    private readonly SecEntityDiscovery _entityDiscovery;
    private readonly Dictionary<Type, SecEntityMetadata> _metadataCache;
    private readonly Dictionary<string, Type> _entityNameToTypeCache;
    private readonly Dictionary<Type, SecPropertyMetadata> _primaryKeyCache;
    private readonly List<Type> _allEntityTypes;

    public SecEntityRegistry(SecEntityDiscovery entityDiscovery)
    {
        _entityDiscovery = entityDiscovery ?? throw new ArgumentNullException(nameof(entityDiscovery));
        _metadataCache = new Dictionary<Type, SecEntityMetadata>();
        _entityNameToTypeCache = new Dictionary<string, Type>(StringComparer.Ordinal);
        _primaryKeyCache = new Dictionary<Type, SecPropertyMetadata>();
        _allEntityTypes = new List<Type>();

        InitializeRegistry();
    }

    /// <summary>
    /// Initializes the registry by discovering and caching all entity metadata.
    /// </summary>
    private void InitializeRegistry()
    {
        var entityTypes = _entityDiscovery.GetEntities();

        foreach (var entityType in entityTypes)
        {
            _allEntityTypes.Add(entityType);

            // Cache by CLR type name (e.g., "Customer")
            var entityName = entityType.Name;
            _entityNameToTypeCache[entityName] = entityType;

            // Pre-load and cache metadata
            var metadata = SecEntityMetadata.FromEntityType(entityType);
            _metadataCache[entityType] = metadata;

            // Cache primary key
            if (metadata.KeyProperty != null)
            {
                _primaryKeyCache[entityType] = metadata.KeyProperty;
            }
        }
    }

    public IReadOnlyList<Type> GetAllEntityTypes()
    {
        return _allEntityTypes.AsReadOnly();
    }

    public bool TryGetEntityType(string entityName, out Type? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            entityType = null;
            return false;
        }

        return _entityNameToTypeCache.TryGetValue(entityName, out entityType);
    }

    public SecEntityMetadata GetEntityMetadata(Type entityType)
    {
        if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

        if (!_metadataCache.TryGetValue(entityType, out var metadata))
        {
            throw new InvalidOperationException(
                $"Entity type '{entityType.FullName}' is not registered as a SecEntity. " +
                "Ensure it is decorated with [SecEntity] and included in entity assemblies.");
        }

        return metadata;
    }

    public SecEntityMetadata GetEntityMetadata(string entityName)
    {
        if (!TryGetEntityType(entityName, out var entityType) || entityType == null)
        {
            throw new InvalidOperationException(
                $"Unknown entity: '{entityName}'. " +
                $"Valid entities are: {string.Join(", ", _entityNameToTypeCache.Keys.OrderBy(x => x))}");
        }

        return GetEntityMetadata(entityType);
    }

    public SecPropertyMetadata GetPrimaryKeyProperty(Type entityType)
    {
        if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

        if (_primaryKeyCache.TryGetValue(entityType, out var keyProperty))
        {
            return keyProperty;
        }

        var metadata = GetEntityMetadata(entityType);
        if (metadata.KeyProperty == null)
        {
            throw new InvalidOperationException(
                $"Entity '{entityType.Name}' does not have a primary key defined. " +
                "Ensure it has an 'Id' property or is decorated with [SecKey].");
        }

        _primaryKeyCache[entityType] = metadata.KeyProperty;
        return metadata.KeyProperty;
    }

    public bool IsOperationAllowed(Type entityType, SecCrud operation)
    {
        if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

        var attr = entityType.GetCustomAttributes(typeof(SecEntityAttribute), false)
            .FirstOrDefault() as SecEntityAttribute;

        if (attr == null)
            return false;

        var allowedCrud = attr.Crud;
        return (allowedCrud & operation) == operation;
    }

    public SecEntityAttribute GetEntityAttribute(Type entityType)
    {
        if (entityType == null)
            throw new ArgumentNullException(nameof(entityType));

        var attr = entityType.GetCustomAttributes(typeof(SecEntityAttribute), false)
            .FirstOrDefault() as SecEntityAttribute;

        if (attr == null)
        {
            throw new InvalidOperationException(
                $"Entity type '{entityType.FullName}' does not have [SecEntity] attribute.");
        }

        return attr;
    }
}
