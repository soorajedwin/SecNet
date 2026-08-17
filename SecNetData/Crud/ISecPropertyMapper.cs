using System;
using System.Collections.Generic;

namespace SecNetData.Crud;

/// <summary>
/// Maps properties from input data to entity objects safely.
/// Respects [SecIgnore], excludes primary keys, only maps allowed scalar properties.
/// </summary>
public interface ISecPropertyMapper
{
    /// <summary>
    /// Maps properties from source object to target entity object.
    /// Only maps scalar properties that are not [SecIgnore] and not the primary key.
    /// </summary>
    /// <param name="source">Source data (typically from JSON deserialization).</param>
    /// <param name="target">Target entity to map to.</param>
    /// <param name="entityRegistry">Entity registry for metadata.</param>
    /// <param name="typeConverter">Type converter for value conversion.</param>
    /// <param name="errors">List of mapping errors.</param>
    /// <returns>True if mapping succeeded, false if there were errors.</returns>
    bool MapProperties(
        object source,
        object target,
        ISecEntityRegistry entityRegistry,
        ISecTypeConverter typeConverter,
        out List<string> errors);
}
