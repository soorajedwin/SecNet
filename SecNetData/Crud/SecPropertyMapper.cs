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
        {
            throw new ArgumentNullException(nameof(entityRegistry));
        }

        if (typeConverter == null)
        {
            throw new ArgumentNullException(nameof(typeConverter));
        }

        var sourceType = source.GetType();
        var targetType = target.GetType();

        var entityMetadata =
            entityRegistry.GetEntityMetadata(targetType);

        var primaryKeyName =
            entityMetadata.KeyProperty?.PropertyName;

        var sourceProperties =
            sourceType.GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance);

        foreach (var sourceProp in sourceProperties)
        {
            if (!sourceProp.CanRead)
            {
                continue;
            }

            // Find target property directly by name.
            var targetProp = targetType.GetProperty(
                sourceProp.Name,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);

            if (targetProp == null || !targetProp.CanWrite)
            {
                continue;
            }

            // Find metadata for this target property.
            var propertyMetadata =
                entityMetadata.Properties.FirstOrDefault(p =>
                    p.PropertyName.Equals(
                        targetProp.Name,
                        StringComparison.OrdinalIgnoreCase));

            // Property is not part of SecEntity metadata.
            if (propertyMetadata == null)
            {
                continue;
            }

            // Respect [SecIgnore].
            if (propertyMetadata.IsIgnored)
            {
                continue;
            }

            // Don't modify primary key.
            if (!string.IsNullOrWhiteSpace(primaryKeyName) &&
                targetProp.Name.Equals(
                    primaryKeyName,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var sourceValue =
                    sourceProp.GetValue(source);

                if (!typeConverter.TryConvert(
                        sourceValue,
                        targetProp.PropertyType,
                        out var convertedValue,
                        out var conversionError))
                {
                    errors.Add(
                        $"Property '{sourceProp.Name}': {conversionError}");

                    continue;
                }

                targetProp.SetValue(
                    target,
                    convertedValue);
            }
            catch (Exception ex)
            {
                errors.Add(
                    $"Property '{sourceProp.Name}': {ex.Message}");
            }
        }

        return errors.Count == 0;
    }
}
