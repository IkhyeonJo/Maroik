using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUnitOfWork"/> that wraps a single
/// <see cref="Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction"/>.
/// Implements <see cref="IAsyncDisposable"/> so that any uncommitted transaction
/// is automatically rolled back if the service scope is disposed without an explicit commit —
/// preventing partial writes in case of unhandled exceptions.
/// </summary>
/// <remarks>
/// One instance is scoped per request and holds at most one live transaction. After
/// <see cref="CommitAsync"/> or <see cref="RollbackAsync"/> the transaction is disposed and the
/// slot cleared, so the same instance can be reused for a second, sequential Begin→Commit cycle
/// within the same request. Nested use is not supported: EF Core has no nested transactions, so
/// calling <see cref="BeginAsync"/> while one is already live throws instead of silently
/// discarding the outer transaction. Commit/Rollback are idempotent no-ops once the transaction
/// has ended, so a <c>catch</c> block can safely call <see cref="RollbackAsync"/> even on a path
/// that already committed.
/// </remarks>
public class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    /// <inheritdoc />
    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException(
                "A transaction is already active on this unit of work. EF Core does not support " +
                "nested transactions — restructure so the outermost caller owns the Begin/Commit, " +
                "or resolve a separate scope for the inner work.");
        }

        _transaction = await context.Database.BeginTransactionAsync(ct);
    }

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            return;

        try
        {
            // Flush the writes the repositories left pending for the duration of the transaction
            // (see GenericRepository) so the whole unit of work reaches the database as one batch,
            // then commit.
            await context.SaveChangesAsync(ct);
            await _transaction.CommitAsync(ct);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction == null)
            return;

        try
        {
            await _transaction.RollbackAsync(ct);
        }
        catch
        {
            // Ignore if the transaction has already been rolled back or committed.
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            // Discard every entity the aborted unit of work left tracked. Since GenericRepository
            // defers its writes while a transaction is open, a rolled-back UpdateEntityAsync /
            // CreateAsync otherwise leaves a Modified/Added entry on the request-scoped DbContext
            // that the next SaveChanges (e.g. a second Begin→Commit cycle in the same request —
            // IncomeService/ExpenditureService adjust-then-fail paths) would silently persist.
            context.ChangeTracker.Clear();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Reached with a non-null transaction only when neither CommitAsync nor RollbackAsync ran
        // (e.g. an unhandled exception escaped the service method) — roll back so nothing partial
        // is left committed.
        if (_transaction != null)
        {
            try { await _transaction.RollbackAsync(); }
            catch { /* Ignore if the transaction has already been rolled back or committed */ }
            await _transaction.DisposeAsync();
            _transaction = null;
            // Same reason as RollbackAsync: drop any deferred writes so they cannot be flushed by
            // a later SaveChanges on the same scoped context.
            context.ChangeTracker.Clear();
        }
        GC.SuppressFinalize(this);
    }
}
