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
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _committed;

    public UnitOfWork(ApplicationDbContext context) => _context = context;

    public async Task BeginAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
        _committed = false;
    }

    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            _committed = true;
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
            await _transaction.RollbackAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            if (!_committed)
            {
                try { await _transaction.RollbackAsync(); }
                catch { /* Ignore if the transaction has already been rolled back or committed */ }
            }
            await _transaction.DisposeAsync();
            _transaction = null;
        }
        GC.SuppressFinalize(this);
    }
}
