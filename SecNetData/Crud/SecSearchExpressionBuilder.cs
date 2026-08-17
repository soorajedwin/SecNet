using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace SecNetData.Crud;

/// <summary>
/// Builds safe EF Core search expressions that filter across string scalar properties.
/// </summary>
public sealed class SecSearchExpressionBuilder : ISecSearchExpressionBuilder
{
    public IQueryable ApplySearch(
        IQueryable query,
        string searchTerm,
        Type entityType,
        ISecEntityRegistry entityRegistry)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        var metadata = entityRegistry.GetEntityMetadata(entityType);

        // Find all string scalar properties that are not [SecIgnore]
        var searchableProps = metadata.Properties
            .Where(p =>
                !p.IsIgnored &&
                p.PropertyType == typeof(string))
            .ToList();

        if (searchableProps.Count == 0)
            return query; // No searchable properties

        try
        {
            // Build a predicate using Where + LINQ operations
            // We'll use EF.Functions.Like for safer contains matching
            var parameter = Expression.Parameter(entityType, "x");
            Expression? orExpression = null;

            foreach (var prop in searchableProps)
            {
                // x.PropertyName
                var propertyAccess = Expression.Property(parameter, prop.PropertyName);

                // EF.Functions.Like(x.PropertyName, "%searchTerm%")
                var likeMethod = typeof(DbFunctionsExtensions).GetMethod(
                    "Like",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null,
                    new[] { typeof(DbFunctions), typeof(string), typeof(string) },
                    null);

                if (likeMethod != null)
                {
                    var dbFunctionsGetter = Expression.Property(null, typeof(EF), "Functions");
                    var likePattern = Expression.Constant($"%{searchTerm}%");
                    var likeCall = Expression.Call(
                        likeMethod,
                        dbFunctionsGetter,
                        propertyAccess,
                        likePattern);

                    orExpression = orExpression == null
                        ? likeCall
                        : Expression.OrElse(orExpression, likeCall);
                }
            }

            if (orExpression == null)
                return query;

            var lambda = Expression.Lambda(orExpression, parameter);

            // Apply Where
            var whereMethod = typeof(Queryable)
                .GetMethods()
                .First(m => m.Name == "Where" && m.GetParameters().Length == 2)
                .MakeGenericMethod(entityType);

            query = (IQueryable)whereMethod.Invoke(null, new object[] { query, lambda })!;
        }
        catch
        {
            // If expression building fails, return unfiltered query
            return query;
        }

        return query;
    }
}

