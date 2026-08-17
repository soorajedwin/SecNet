using System.Reflection;
using SecNetCore.Attributes;

namespace SecNetData.Schema;

/// <summary>
/// Encapsulates metadata about a SecEntity class.
/// </summary>
public sealed class SecEntityMetadata
{
    /// <summary>
    /// The CLR type of the entity.
    /// </summary>
    public Type EntityType { get; set; } = null!;

    /// <summary>
    /// The table name in the database. Respects [SecEntity("table_name")] attribute or conventions.
    /// </summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>
    /// All properties of the entity (excluding ignored ones).
    /// </summary>
    public List<SecPropertyMetadata> Properties { get; set; } = new();

    /// <summary>
    /// The primary key property, if one exists.
    /// </summary>
    public SecPropertyMetadata? KeyProperty { get; set; }

    /// <summary>
    /// Creates metadata from an entity type decorated with [SecEntity].
    /// </summary>
    public static SecEntityMetadata FromEntityType(Type entityType)
    {
        var attr = entityType.GetCustomAttribute<SecEntityAttribute>();
        if (attr == null)
        {
            throw new InvalidOperationException(
                $"Type '{entityType.FullName}' is not decorated with [SecEntity].");
        }

        var metadata = new SecEntityMetadata
        {
            EntityType = entityType,
            TableName = !string.IsNullOrWhiteSpace(attr.TableName)
                ? attr.TableName
                : entityType.Name,
        };

        // Extract all public instance properties
        var properties = entityType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();

        foreach (var prop in properties)
        {
            var propMetadata = SecPropertyMetadata.FromPropertyInfo(prop);

            // Skip ignored properties
            if (propMetadata.IsIgnored)
            {
                continue;
            }

            metadata.Properties.Add(propMetadata);

            // Track the primary key
            if (propMetadata.IsKey || prop.Name == "Id")
            {
                metadata.KeyProperty = propMetadata;
            }
        }

        // Ensure we have a primary key; default to "Id" convention
        if (metadata.KeyProperty == null)
        {
            var idProp = metadata.Properties.FirstOrDefault(p => p.PropertyName == "Id");
            if (idProp != null)
            {
                idProp.IsKey = true;
                metadata.KeyProperty = idProp;
            }
        }

        return metadata;
    }
}
