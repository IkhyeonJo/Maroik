namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Unit-of-Work abstraction that wraps a database transaction.
/// Implement the using-await pattern: begin → execute repository calls → commit (or rollback on error).
/// Inherits <see cref="IAsyncDisposable"/> so the transaction is automatically rolled back
/// if <see cref="CommitAsync"/> was never called.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>Opens a new database transaction.</summary>
    Task BeginAsync(CancellationToken ct = default);

    /// <summary>
    /// Flushes every change tracked by the current context to the database without ending the
    /// transaction. <see cref="CommitAsync"/> already does this, so call it directly only when a
    /// generated key is needed before the transaction is committed.
    /// </summary>
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Flushes any pending changes and commits the current transaction.</summary>
    Task CommitAsync(CancellationToken ct = default);

    /// <summary>Rolls back all changes made within the current transaction.</summary>
    Task RollbackAsync(CancellationToken ct = default);
}
