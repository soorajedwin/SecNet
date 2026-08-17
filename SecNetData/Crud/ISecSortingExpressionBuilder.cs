using System;
using System.Linq;
using SecNetCore.Models;

namespace SecNetData.Crud;

/// <summary>
/// Builds safe EF Core sorting expressions from SecSort requests.
/// </summary>
public interface ISecSortingExpressionBuilder
{
    /// <summary>
    /// Applies sorting to an IQueryable based on SecSort parameters.
    /// Only allows sorting on valid scalar properties defined in entity metadata.
    /// </summary>
    /// <param name="query">The EF Core query.</param>
    /// <param name="sorts">List of sort specifications.</param>
    /// <param name="entityType">The entity type being queried.</param>
    /// <param name="entityRegistry">Entity registry for property validation.</param>
    /// <param name="errors">List of validation errors if any prop is invalid.</param>
    /// <returns>The query with sorting applied, or original query if no sorts.</returns>
    IQueryable ApplySorting(
        IQueryable query,
        List<SecSort> sorts,
        Type entityType,
        ISecEntityRegistry entityRegistry,
        out List<string> errors);
}
