using System;
using System.Linq;

namespace SecNetData.Crud;

/// <summary>
/// Builds safe EF Core search expressions to filter entities by search term.
/// </summary>
public interface ISecSearchExpressionBuilder
{
    /// <summary>
    /// Applies a generic search filter across string scalar properties.
    /// </summary>
    /// <param name="query">The EF Core query.</param>
    /// <param name="searchTerm">The search term (e.g., "john" to search Name, Email, etc.).</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="entityRegistry">Entity registry for property metadata.</param>
    /// <returns>The query with search filter applied.</returns>
    IQueryable ApplySearch(
        IQueryable query,
        string searchTerm,
        Type entityType,
        ISecEntityRegistry entityRegistry);
}
