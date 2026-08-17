using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SecNetCore.Models;

namespace SecNetData.Crud;

/// <summary>
/// Builds safe EF Core sorting expressions from SecSort requests.
/// </summary>
public sealed class SecSortingExpressionBuilder : ISecSortingExpressionBuilder
{
    public IQueryable ApplySorting(
        IQueryable query,
        List<SecSort> sorts,
        Type entityType,
        ISecEntityRegistry entityRegistry,
        out List<string> errors)
    {
        errors = new List<string>();

        if (sorts == null || sorts.Count == 0)
            return query;

        var metadata = entityRegistry.GetEntityMetadata(entityType);

        foreach (var sort in sorts)
        {
            if (string.IsNullOrWhiteSpace(sort.Property))
                continue;

            // Verify the property exists and is not [SecIgnore]
            var propMetadata = metadata.Properties.FirstOrDefault(p =>
                p.PropertyName.Equals(sort.Property, StringComparison.OrdinalIgnoreCase));

            if (propMetadata == null)
            {
                errors.Add($"Property '{sort.Property}' does not exist on entity '{entityType.Name}'.");
                continue;
            }

            if (propMetadata.IsIgnored)
            {
                errors.Add($"Property '{sort.Property}' is ignored and cannot be sorted.");
                continue;
            }

            // Build the sorting expression using EF.Property for generic property access
            var parameter = Expression.Parameter(entityType, "x");
            var property = Expression.Property(parameter, propMetadata.PropertyName);
            var lambda = Expression.Lambda(property, parameter);

            // Apply OrderBy or ThenBy
            string methodName = sort.Descending ? "OrderByDescending" : "OrderBy";

            try
            {
                query = typeof(Queryable)
                    .GetMethods()
                    .First(m => m.Name == methodName && m.GetParameters().Length == 2)
                    .MakeGenericMethod(entityType, property.Type)
                    .Invoke(null, new object[] { query, lambda }) as IQueryable
                    ?? query;
            }
            catch (Exception ex)
            {
                errors.Add($"Error sorting by property '{sort.Property}': {ex.Message}");
            }
        }

        return query;
    }
}
