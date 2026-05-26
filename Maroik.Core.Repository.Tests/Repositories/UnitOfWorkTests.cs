using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="UnitOfWork"/>.
/// Only the null-guard / no-transaction paths are tested here because those exercise our
/// code (the <c>if (_transaction != null)</c> guards and the <c>_committed</c> flag).
/// Tests that require an actual database transaction are not included — they would only
/// verify that EF Core's own transaction API works, not our code.
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes the test fixture with an EF Core InMemory database.</summary>
    public UnitOfWorkTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    /// <summary>Releases resources allocated by this test fixture.</summary>
    public void Dispose() => _context.Dispose();

    // ── CommitAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>CommitAsync</c> does not throw when begin was not called.</summary>
    [Fact]
    public async Task CommitAsync_DoesNotThrow_WhenBeginWasNotCalled()
    {
        await using var uow = new UnitOfWork(_context);

        var ex = await Record.ExceptionAsync(() => uow.CommitAsync());

        Assert.Null(ex);
    }

    // ── RollbackAsync ────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>RollbackAsync</c> does not throw when begin was not called.</summary>
    [Fact]
    public async Task RollbackAsync_DoesNotThrow_WhenBeginWasNotCalled()
    {
        await using var uow = new UnitOfWork(_context);

        var ex = await Record.ExceptionAsync(() => uow.RollbackAsync());

        Assert.Null(ex);
    }

    // ── DisposeAsync ─────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DisposeAsync</c> does not throw when no transaction was started.</summary>
    [Fact]
    public async Task DisposeAsync_DoesNotThrow_WhenNoTransactionWasStarted()
    {
        var uow = new UnitOfWork(_context);

        var ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

        Assert.Null(ex);
    }
}
