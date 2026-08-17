using System.Reflection;
using System.ComponentModel.DataAnnotations;
using SecNetCore.Attributes;

namespace SecNetData.Schema;

/// <summary>
/// Encapsulates metadata about a single property of a SecEntity.
/// </summary>
public sealed class SecPropertyMetadata
{
    /// <summary>
    /// The property name in the CLR class.
    /// </summary>
    public string PropertyName { get; set; } = string.Empty;

    /// <summary>
    /// The column name in the database. Respects [SecColumn("name")] attribute.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// The CLR type of the property.
    /// </summary>
    public Type PropertyType { get; set; } = null!;

    /// <summary>
    /// Whether this property represents the primary key.
    /// </summary>
    public bool IsKey { get; set; }

    /// <summary>
    /// Whether this property should be ignored by EF Core.
    /// Respects [SecIgnore] attribute.
    /// </summary>
    public bool IsIgnored { get; set; }

    /// <summary>
    /// Whether this property is nullable.
    /// Determined from CLR nullable reference types or [Required] absence.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Maximum length if [MaxLength] is applied.
    /// </summary>
    public int? MaxLength { get; set; }

    /// <summary>
    /// Whether [Required] attribute is present.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Creates metadata from a PropertyInfo with SecEntity reflection.
    /// </summary>
    public static SecPropertyMetadata FromPropertyInfo(PropertyInfo prop)
    {
        var metadata = new SecPropertyMetadata
        {
            PropertyName = prop.Name,
            PropertyType = prop.PropertyType,
            IsIgnored = prop.GetCustomAttribute<SecIgnoreAttribute>() != null,
            IsKey = prop.GetCustomAttribute<SecKeyAttribute>() != null,
        };

        // Respect [SecColumn("name")] for custom column name
        var secColumnAttr = prop.GetCustomAttribute<SecColumnAttribute>();
        metadata.ColumnName =
            !string.IsNullOrWhiteSpace(secColumnAttr?.Name)
                ? secColumnAttr.Name
                : prop.Name;

        // Check for [Required] attribute
        metadata.IsRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;

        // Check for [MaxLength] attribute
        var maxLengthAttr = prop.GetCustomAttribute<MaxLengthAttribute>();
        if (maxLengthAttr != null)
        {
            metadata.MaxLength = maxLengthAttr.Length;
        }

        // Determine nullability
        // A property is considered nullable if:
        // 1. It's not marked [Required]
        // 2. The CLR type is nullable (Nullable<T> or nullable reference type)
        var underlyingType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

        // For reference types, check if they allow nullable (null-forgiving operator or NullableAttribute)
        bool isReferenceType = !underlyingType.IsValueType;
        bool isNullableType = Nullable.GetUnderlyingType(prop.PropertyType) != null;

        // If it's a reference type and not [Required], assume nullable
        // If it's a value type and nullable (Nullable<T>), it's nullable
        metadata.IsNullable = isNullableType || (isReferenceType && !metadata.IsRequired);

        return metadata;
    }
}
