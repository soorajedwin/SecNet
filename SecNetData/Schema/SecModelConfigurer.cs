using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecNetCore.Attributes;
using SecNetData.Services;
using System.Reflection;

namespace SecNetData.Schema;

/// <summary>
/// Configures EF Core ModelBuilder with discovered SecEntity types.
/// </summary>
public sealed class SecModelConfigurer
{
    private readonly SecEntityDiscovery _entityDiscovery;
    private readonly Dictionary<Type, SecEntityMetadata> _metadataCache = new();

    public SecModelConfigurer(SecEntityDiscovery entityDiscovery)
    {
        _entityDiscovery = entityDiscovery
            ?? throw new ArgumentNullException(nameof(entityDiscovery));
    }

    /// <summary>
    /// Configures all discovered SecEntity types in the ModelBuilder.
    /// </summary>
    public void ConfigureModel(ModelBuilder modelBuilder)
    {
        if (modelBuilder == null)
        {
            throw new ArgumentNullException(nameof(modelBuilder));
        }

        var entityTypes = _entityDiscovery.GetEntities();

        foreach (var entityType in entityTypes)
        {
            ConfigureEntity(modelBuilder, entityType);
        }
    }

    /// <summary>
    /// Gets or creates cached metadata for an entity type.
    /// </summary>
    private SecEntityMetadata GetEntityMetadata(Type entityType)
    {
        if (_metadataCache.TryGetValue(entityType, out var cached))
        {
            return cached;
        }

        var metadata = SecEntityMetadata.FromEntityType(entityType);
        _metadataCache[entityType] = metadata;
        return metadata;
    }

    /// <summary>
    /// Configures a single entity type with EF Core.
    /// </summary>
    private void ConfigureEntity(ModelBuilder modelBuilder, Type entityType)
    {
        var metadata = GetEntityMetadata(entityType);

        // Use Entity<T>().ToTable() to set table name and configure entity
        var entityMethod = typeof(ModelBuilder)
            .GetMethod(nameof(ModelBuilder.Entity), System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new[] { typeof(string) }, null);

        if (entityMethod == null)
        {
            throw new InvalidOperationException("Could not find ModelBuilder.Entity method.");
        }

        // Call modelBuilder.Entity<T>()
        var entityBuilderType = typeof(EntityTypeBuilder<>).MakeGenericType(entityType);
        var genericEntityMethod = typeof(ModelBuilder)
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "Entity" && m.IsGenericMethod && m.GetGenericArguments().Length == 1);

        if (genericEntityMethod == null)
        {
            throw new InvalidOperationException("Could not find generic Entity method.");
        }

        var entityBuilder = genericEntityMethod
            .MakeGenericMethod(entityType)
            .Invoke(modelBuilder, Array.Empty<object>());

        if (entityBuilder == null)
        {
            throw new InvalidOperationException("Entity builder is null.");
        }

        // Configure table name
        var toTableMethod = entityBuilderType.GetMethod(
            "ToTable",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
            null,
            new[] { typeof(string) },
            null);

        toTableMethod?.Invoke(entityBuilder, new object[] { metadata.TableName });

        var ignoredProperties = entityType
            .GetProperties(
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance)
            .Where(p =>
                p.GetCustomAttribute<SecIgnoreAttribute>() != null);

        foreach (var ignoredProperty in ignoredProperties)
        {
            var ignoreMethod = entityBuilderType.GetMethod(
                "Ignore",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

            ignoreMethod?.Invoke(
                entityBuilder,
                new object[] { ignoredProperty.Name });
        }

        // Configure properties
        foreach (var propMetadata in metadata.Properties)
        {
            ConfigureProperty(entityBuilder, entityBuilderType, propMetadata, metadata);
        }

        // Configure key
        if (metadata.KeyProperty != null)
        {
            ConfigureKey(entityBuilder, entityBuilderType, metadata.KeyProperty);
        }
    }

    /// <summary>
    /// Configures a single property with EF Core.
    /// </summary>
    private void ConfigureProperty(
        object entityBuilder,
        Type entityBuilderType,
        SecPropertyMetadata propMetadata,
        SecEntityMetadata entityMetadata)
    {
        // Get the Property<T>() method: entityBuilder.Property<PropertyType>("PropertyName")
        var propertyMethod = entityBuilderType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "Property" &&
                m.IsGenericMethod &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters() is var parms &&
                parms.Length == 1 &&
                parms[0].ParameterType == typeof(string));

        if (propertyMethod == null)
        {
            return;
        }

        var propertyBuilderInstance = propertyMethod
            .MakeGenericMethod(propMetadata.PropertyType)
            .Invoke(entityBuilder, new object[] { propMetadata.PropertyName });

        if (propertyBuilderInstance == null)
        {
            return;
        }

        var propertyBuilderType = propertyBuilderInstance.GetType();

        // Configure HasColumnName
        if (propMetadata.ColumnName != propMetadata.PropertyName)
        {
            var hasColumnNameMethod = propertyBuilderType.GetMethod(
                "HasColumnName",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(string) },
                null);

            hasColumnNameMethod?.Invoke(propertyBuilderInstance, new object[] { propMetadata.ColumnName });
        }

        // Configure IsRequired
        if (propMetadata.IsRequired)
        {
            var isRequiredMethod = propertyBuilderType.GetMethod(
                "IsRequired",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                Array.Empty<Type>(),
                null);

            isRequiredMethod?.Invoke(propertyBuilderInstance, Array.Empty<object>());
        }

        // Configure HasMaxLength
        if (propMetadata.MaxLength.HasValue)
        {
            var hasMaxLengthMethod = propertyBuilderType.GetMethod(
                "HasMaxLength",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null,
                new[] { typeof(int) },
                null);

            hasMaxLengthMethod?.Invoke(propertyBuilderInstance, new object[] { propMetadata.MaxLength.Value });
        }
    }

    /// <summary>
    /// Configures the primary key for an entity.
    /// </summary>
    private void ConfigureKey(
        object entityBuilder,
        Type entityBuilderType,
        SecPropertyMetadata keyPropMetadata)
    {
        // Get the HasKey<T>() method where T matches the key property type
        var hasKeyMethod = entityBuilderType
            .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .FirstOrDefault(m =>
                m.Name == "HasKey" &&
                m.IsGenericMethod &&
                m.GetGenericArguments().Length == 1 &&
                m.GetParameters() is var parms &&
                parms.Length == 1 &&
                parms[0].ParameterType == typeof(string));

        if (hasKeyMethod != null)
        {
            hasKeyMethod
                .MakeGenericMethod(keyPropMetadata.PropertyType)
                .Invoke(entityBuilder, new object[] { keyPropMetadata.PropertyName });
        }
    }
}
