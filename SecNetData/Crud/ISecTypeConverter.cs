using System;

namespace SecNetData.Crud;

/// <summary>
/// Service for safely converting values to target CLR types.
/// Supports string, bool, numeric, Guid, DateTime, and nullable types.
/// </summary>
public interface ISecTypeConverter
{
    /// <summary>
    /// Attempts to convert a value to the target CLR type.
    /// </summary>
    /// <param name="value">The value to convert (typically from JSON).</param>
    /// <param name="targetType">The target CLR type.</param>
    /// <param name="result">The converted result, or default if conversion failed.</param>
    /// <param name="error">Error message if conversion failed.</param>
    /// <returns>True if conversion was successful, false otherwise.</returns>
    bool TryConvert(object? value, Type targetType, out object? result, out string? error);
}
