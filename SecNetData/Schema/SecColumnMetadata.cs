using System;

namespace SecNetData.Schema;

/// <summary>
/// Represents the expected schema metadata for a single database column.
/// Derived from entity property metadata.
/// </summary>
public sealed class SecColumnMetadata
{
    /// <summary>
    /// The column name in the MySQL database.
    /// </summary>
    public string ColumnName { get; set; } = string.Empty;

    /// <summary>
    /// The MySQL column type definition (e.g., "INT", "VARCHAR(150)").
    /// </summary>
    public string MySQLType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the column allows NULL values.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Whether this column is the primary key.
    /// </summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>
    /// Whether this column should have a UNIQUE constraint.
    /// Typically not set by default; can be extended for future use.
    /// </summary>
    public bool IsUnique { get; set; }

    /// <summary>
    /// Whether this column should be auto-incrementing.
    /// Typically used for integer primary keys.
    /// </summary>
    public bool IsAutoIncrement { get; set; }

    /// <summary>
    /// The default value for the column, if any.
    /// Null means no default.
    /// </summary>
    public string? DefaultValue { get; set; }

    /// <summary>
    /// Creates column metadata from property metadata.
    /// </summary>
    public static SecColumnMetadata FromPropertyMetadata(
        SecPropertyMetadata propMetadata,
        MySQLTypeMapper typeMapper)
    {
        if (propMetadata == null)
            throw new ArgumentNullException(nameof(propMetadata));

        if (typeMapper == null)
            throw new ArgumentNullException(nameof(typeMapper));

        var mysqlType = typeMapper.GetColumnType(
            propMetadata.PropertyType,
            propMetadata.MaxLength,
            propMetadata.IsNullable);

        var columnMetadata = new SecColumnMetadata
        {
            ColumnName = propMetadata.ColumnName,
            MySQLType = mysqlType,
            IsNullable = propMetadata.IsNullable && !propMetadata.IsRequired,
            IsPrimaryKey = propMetadata.IsKey,
            IsAutoIncrement = propMetadata.IsKey && IsAutoIncrementType(propMetadata.PropertyType),
        };

        return columnMetadata;
    }

    /// <summary>
    /// Determines if a type should use AUTO_INCREMENT for primary keys.
    /// </summary>
    private static bool IsAutoIncrementType(Type clrType)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;
        return underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte);
    }
}
