using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SecNetData.Crud;

/// <summary>
/// Implementation of ISecPropertyMapper that safely maps properties from input to entities.
/// </summary>
public sealed class SecPropertyMapper : ISecPropertyMapper
{
    public bool MapProperties(
        object source,
        object target,
        ISecEntityRegistry entityRegistry,
        ISecTypeConverter typeConverter,
        out List<string> errors)
    {
        errors = new List<string>();

        if (source == null)
        {
            errors.Add("Source object cannot be null.");
            return false;
        }

        if (target == null)
        {
            errors.Add("Target entity cannot be null.");
            return false;
        }

        if (entityRegistry == null)
            throw new ArgumentNullException(nameof(entityRegistry));

        if (typeConverter == null)
            throw new ArgumentNullException(nameof(typeConverter));

        var targetType = target.GetType();
        var entityMetadata = entityRegistry.GetEntityMetadata(targetType);
        var primaryKeyProperty = entityMetadata.KeyProperty;

        // Get all properties from the source object
        var sourceProperties = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.IgnoreCase);

        foreach (var sourceProp in sourceProperties)
        {
            if (!sourceProp.CanRead)
                continue;

            // Find matching property in entity metadata by name (case-insensitive)
            var matchingProp = entityMetadata.Properties.FirstOrDefault(p =>
                p.PropertyName.Equals(sourceProp.Name, StringComparison.OrdinalIgnoreCase));

            if (matchingProp == null)
            {
                // Property not in entity metadata - skip it
                continue;
            }

            // Skip [SecIgnore] properties
            if (matchingProp.IsIgnored)
                continue;

            // Skip the primary key (it should not be modified)
            if (primaryKeyProperty != null && 
                matchingProp.PropertyName.Equals(primaryKeyProperty.PropertyName, StringComparison.Ordinal))
            {
                continue;
            }

            // Get the target property
            var targetProp = targetType.GetProperty(matchingProp.PropertyName, BindingFlags.Public | BindingFlags.IgnoreCase);
            if (targetProp == null || !targetProp.CanWrite)
            {
                continue;
            }

            try
            {
                // Get the source value
                var sourceValue = sourceProp.GetValue(source);

                // Convert the value to the target property type
                if (!typeConverter.TryConvert(sourceValue, targetProp.PropertyType, out var convertedValue, out var conversionError))
                {
                    errors.Add($"Property '{sourceProp.Name}': {conversionError}");
                    continue;
                }

                // Set the value on the target
                targetProp.SetValue(target, convertedValue);
            }
            catch (Exception ex)
            {
                errors.Add($"Property '{sourceProp.Name}': {ex.Message}");
            }
        }

        return errors.Count == 0;
    }
}
