using System;
using System.Threading;
using System.Threading.Tasks;

namespace SecNetData.Schema;

/// <summary>
/// Provides schema synchronization services for SecEntity models.
/// Automatically creates/updates MySQL schema based on discovered entities.
/// </summary>
public interface ISecSchemaManager
{
    /// <summary>
    /// Synchronizes the database schema with the application's SecEntity models.
    /// This includes creating the database, tables, and columns as needed.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the synchronization operation.</returns>
    Task<SecSchemaResult> SynchronizeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether the database schema is fully synchronized with the models.
    /// Does not make any changes, only reports differences.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result detailing any schema differences found.</returns>
    Task<SecSchemaResult> CheckSchemaAsync(CancellationToken cancellationToken = default);
}
