using System;
using System.Text.Json;

namespace SecNetData.Crud;

/// <summary>
/// Safely converts values from JSON (typically strings) to target CLR types.
/// </summary>
public sealed class SecTypeConverter : ISecTypeConverter
{
    public bool TryConvert(object? value, Type targetType, out object? result, out string? error)
    {
        result = null;
        error = null;

        if (targetType == null)
        {
            error = "Target type cannot be null.";
            return false;
        }

        // Handle null values
        if (value == null)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
            if (underlyingType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
            {
                // Non-nullable value type
                error = $"Cannot convert null to non-nullable type '{targetType.Name}'.";
                return false;
            }
            result = null;
            return true;
        }

        if (value is JsonElement jsonElement)
        {
            return TryConvertJsonElement(
                jsonElement,
                targetType,
                out result,
                out error);
        }

        // If value is already the target type, return it
        if (targetType.IsAssignableFrom(value.GetType()))
        {
            result = value;
            return true;
        }

        // Get the underlying type for nullable types
        var actualTargetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            // Handle specific types
            if (actualTargetType == typeof(string))
            {
                result = value.ToString();
                return true;
            }

            if (actualTargetType == typeof(bool))
            {
                if (bool.TryParse(value.ToString(), out bool boolResult))
                {
                    result = boolResult;
                    return true;
                }
                // Try to parse as 0/1
                if (int.TryParse(value.ToString(), out int intVal))
                {
                    result = intVal != 0;
                    return true;
                }
                error = $"Cannot convert '{value}' to bool.";
                return false;
            }

            if (actualTargetType == typeof(byte))
            {
                if (byte.TryParse(value.ToString(), out byte byteResult))
                {
                    result = byteResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to byte.";
                return false;
            }

            if (actualTargetType == typeof(short))
            {
                if (short.TryParse(value.ToString(), out short shortResult))
                {
                    result = shortResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to short.";
                return false;
            }

            if (actualTargetType == typeof(int))
            {
                if (int.TryParse(value.ToString(), out int intResult))
                {
                    result = intResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to int.";
                return false;
            }

            if (actualTargetType == typeof(long))
            {
                if (long.TryParse(value.ToString(), out long longResult))
                {
                    result = longResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to long.";
                return false;
            }

            if (actualTargetType == typeof(float))
            {
                if (float.TryParse(value.ToString(), out float floatResult))
                {
                    result = floatResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to float.";
                return false;
            }

            if (actualTargetType == typeof(double))
            {
                if (double.TryParse(value.ToString(), out double doubleResult))
                {
                    result = doubleResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to double.";
                return false;
            }

            if (actualTargetType == typeof(decimal))
            {
                if (decimal.TryParse(value.ToString(), out decimal decimalResult))
                {
                    result = decimalResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to decimal.";
                return false;
            }

            if (actualTargetType == typeof(Guid))
            {
                if (Guid.TryParse(value.ToString(), out var guidResult))
                {
                    result = guidResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to Guid.";
                return false;
            }

            if (actualTargetType == typeof(DateTime))
            {
                if (DateTime.TryParse(value.ToString(), out var dateTimeResult))
                {
                    result = dateTimeResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to DateTime.";
                return false;
            }

            if (actualTargetType == typeof(DateTimeOffset))
            {
                if (DateTimeOffset.TryParse(value.ToString(), out var dateTimeOffsetResult))
                {
                    result = dateTimeOffsetResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to DateTimeOffset.";
                return false;
            }

            if (actualTargetType == typeof(DateOnly))
            {
                if (DateOnly.TryParse(value.ToString(), out var dateOnlyResult))
                {
                    result = dateOnlyResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to DateOnly.";
                return false;
            }

            if (actualTargetType == typeof(TimeOnly))
            {
                if (TimeOnly.TryParse(value.ToString(), out var timeOnlyResult))
                {
                    result = timeOnlyResult;
                    return true;
                }
                error = $"Cannot convert '{value}' to TimeOnly.";
                return false;
            }

            // Handle enums
            if (actualTargetType.IsEnum)
            {
                try
                {
                    result = Enum.Parse(actualTargetType, value.ToString()!, ignoreCase: true);
                    return true;
                }
                catch
                {
                    error = $"Cannot convert '{value}' to enum type '{actualTargetType.Name}'.";
                    return false;
                }
            }

            // Default: try using Convert.ChangeType
            result = Convert.ChangeType(value, actualTargetType);
            return true;
        }
        catch (Exception ex)
        {
            error = $"Conversion error: {ex.Message}";
            return false;
        }
    }

    private bool TryConvertJsonElement(
    JsonElement element,
    Type targetType,
    out object? result,
    out string? error)
    {
        result = null;
        error = null;

        if (element.ValueKind == JsonValueKind.Null)
        {
            return TryConvert(
                null,
                targetType,
                out result,
                out error);
        }

        var actualTargetType =
            Nullable.GetUnderlyingType(targetType) ?? targetType;

        try
        {
            if (actualTargetType == typeof(string))
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    result = element.GetString();
                    return true;
                }

                result = element.ToString();
                return true;
            }

            if (actualTargetType == typeof(bool))
            {
                if (element.ValueKind == JsonValueKind.True ||
                    element.ValueKind == JsonValueKind.False)
                {
                    result = element.GetBoolean();
                    return true;
                }

                return TryConvert(
                    element.ToString(),
                    targetType,
                    out result,
                    out error);
            }

            if (actualTargetType == typeof(byte))
            {
                if (element.TryGetByte(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(short))
            {
                if (element.TryGetInt16(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(int))
            {
                if (element.TryGetInt32(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(long))
            {
                if (element.TryGetInt64(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(float))
            {
                if (element.TryGetSingle(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(double))
            {
                if (element.TryGetDouble(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(decimal))
            {
                if (element.TryGetDecimal(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(Guid))
            {
                if (element.ValueKind == JsonValueKind.String &&
                    element.TryGetGuid(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(DateTime))
            {
                if (element.ValueKind == JsonValueKind.String &&
                    element.TryGetDateTime(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(DateTimeOffset))
            {
                if (element.ValueKind == JsonValueKind.String &&
                    element.TryGetDateTimeOffset(out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(DateOnly))
            {
                if (element.ValueKind == JsonValueKind.String &&
                    DateOnly.TryParse(
                        element.GetString(),
                        out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType == typeof(TimeOnly))
            {
                if (element.ValueKind == JsonValueKind.String &&
                    TimeOnly.TryParse(
                        element.GetString(),
                        out var value))
                {
                    result = value;
                    return true;
                }
            }

            if (actualTargetType.IsEnum)
            {
                if (element.ValueKind == JsonValueKind.String)
                {
                    result = Enum.Parse(
                        actualTargetType,
                        element.GetString()!,
                        ignoreCase: true);

                    return true;
                }

                if (element.ValueKind == JsonValueKind.Number &&
                    element.TryGetInt32(out var enumValue))
                {
                    result = Enum.ToObject(
                        actualTargetType,
                        enumValue);

                    return true;
                }
            }

            // Complex types / collections / objects.
            result = JsonSerializer.Deserialize(
                element.GetRawText(),
                targetType);

            if (result != null)
            {
                return true;
            }

            error =
                $"Cannot convert JSON value to '{targetType.Name}'.";

            return false;
        }
        catch (Exception ex)
        {
            error =
                $"Cannot convert JSON value to '{targetType.Name}': {ex.Message}";

            return false;
        }
    }
}
