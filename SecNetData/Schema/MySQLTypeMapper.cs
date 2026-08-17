using System;
using System.Collections.Generic;

namespace SecNetData.Schema;

/// <summary>
/// Maps CLR types to MySQL column type definitions.
/// Handles precision/scale for numeric types and length constraints for strings.
/// </summary>
public sealed class MySQLTypeMapper
{
    private static readonly Dictionary<Type, string> TypeMapping = new()
    {
        // Value types
        { typeof(bool), "BOOLEAN" },
        { typeof(byte), "TINYINT UNSIGNED" },
        { typeof(sbyte), "TINYINT" },
        { typeof(short), "SMALLINT" },
        { typeof(ushort), "SMALLINT UNSIGNED" },
        { typeof(int), "INT" },
        { typeof(uint), "INT UNSIGNED" },
        { typeof(long), "BIGINT" },
        { typeof(ulong), "BIGINT UNSIGNED" },
        { typeof(float), "FLOAT" },
        { typeof(double), "DOUBLE" },
        { typeof(decimal), "DECIMAL(18, 2)" }, // Default, can be customized via metadata

        // Reference types
        { typeof(string), "VARCHAR(255)" }, // Default, respects MaxLength
        { typeof(char), "CHAR(1)" },

        // Date/Time types
        { typeof(DateTime), "DATETIME" },
        { typeof(DateTimeOffset), "DATETIME" },
        { typeof(DateOnly), "DATE" },
        { typeof(TimeOnly), "TIME" },

        // Other types
        { typeof(Guid), "CHAR(36)" }, // UUID string format
        { typeof(byte[]), "LONGBLOB" },
    };

    /// <summary>
    /// Gets the MySQL column type for a given CLR type.
    /// Applies constraints from property metadata where applicable.
    /// </summary>
    public string GetColumnType(
        Type clrType,
        int? maxLength = null,
        bool isNullable = true)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        if (!TypeMapping.TryGetValue(underlyingType, out var mysqlType))
        {
            // Default to TEXT for unknown types
            return "LONGTEXT";
        }

        // Apply constraints for string types
        if (underlyingType == typeof(string))
        {
            if (maxLength.HasValue && maxLength.Value > 0)
            {
                // Use VARCHAR with specific length
                return $"VARCHAR({maxLength.Value})";
            }

            // Default for strings without explicit length
            return mysqlType;
        }

        return mysqlType;
    }

    /// <summary>
    /// Determines whether a type is a numeric type.
    /// </summary>
    public bool IsNumericType(Type clrType)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        return underlyingType == typeof(byte) ||
               underlyingType == typeof(sbyte) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(ushort) ||
               underlyingType == typeof(int) ||
               underlyingType == typeof(uint) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(ulong) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(decimal);
    }

    /// <summary>
    /// Determines whether a type is a date/time type.
    /// </summary>
    public bool IsDateTimeType(Type clrType)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;

        return underlyingType == typeof(DateTime) ||
               underlyingType == typeof(DateTimeOffset) ||
               underlyingType == typeof(DateOnly) ||
               underlyingType == typeof(TimeOnly);
    }

    /// <summary>
    /// Determines whether a type is a string type.
    /// </summary>
    public bool IsStringType(Type clrType)
    {
        var underlyingType = Nullable.GetUnderlyingType(clrType) ?? clrType;
        return underlyingType == typeof(string);
    }
}
