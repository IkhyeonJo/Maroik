using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="UnitOfWork"/>.
/// SQLite in-memory is used because the EF Core InMemory provider does not support transactions
/// (<c>BeginTransactionAsync</c> throws for the InMemory provider).
/// </summary>
public class UnitOfWorkTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public UnitOfWorkTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new ApplicationDbContext(options);
        _context.Database.EnsureCreated();
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ── BeginAsync ───────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>BeginAsync</c> does not throw.</summary>
    [Fact]
    public async Task BeginAsync_DoesNotThrow()
    {
        await using var uow = new UnitOfWork(_context);

        var ex = await Record.ExceptionAsync(() => uow.BeginAsync());

        Assert.Null(ex);
    }

    // ── CommitAsync ──────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>CommitAsync</c> commits data to database.</summary>
    [Fact]
    public async Task CommitAsync_CommitsDataToDatabase()
    {
        await using var uow = new UnitOfWork(_context);
        await uow.BeginAsync();

        // AddAsync explicitly marks the entity as Added — no ChangeTracker tweak needed.
        await _context.CalendarShareds.AddAsync(new CalendarShared { CalendarId = 1, User = true, Anonymous = false }, TestContext.Current.CancellationToken);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await uow.CommitAsync();

        bool exists = await _context.CalendarShareds.AnyAsync(cs => cs.CalendarId == 1, TestContext.Current.CancellationToken);
        Assert.True(exists);
    }

    /// <summary>Verifies that <c>CommitAsync</c> does not throw when begin was not called.</summary>
    [Fact]
    public async Task CommitAsync_DoesNotThrow_WhenBeginWasNotCalled()
    {
        await using var uow = new UnitOfWork(_context);

        // CommitAsync with no active transaction should be a no-op (transaction is null)
        var ex = await Record.ExceptionAsync(() => uow.CommitAsync());

        Assert.Null(ex);
    }

    // ── RollbackAsync ────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>RollbackAsync</c> does not throw.</summary>
    [Fact]
    public async Task RollbackAsync_DoesNotThrow()
    {
        await using var uow = new UnitOfWork(_context);
        await uow.BeginAsync();

        var ex = await Record.ExceptionAsync(() => uow.RollbackAsync());

        Assert.Null(ex);
    }

    /// <summary>Verifies that <c>RollbackAsync</c> does not throw when begin was not called.</summary>
    [Fact]
    public async Task RollbackAsync_DoesNotThrow_WhenBeginWasNotCalled()
    {
        await using var uow = new UnitOfWork(_context);

        // RollbackAsync with no active transaction should be a no-op (transaction is null)
        var ex = await Record.ExceptionAsync(() => uow.RollbackAsync());

        Assert.Null(ex);
    }

    // ── DisposeAsync ─────────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DisposeAsync</c> auto rolls back when not committed.</summary>
    [Fact]
    public async Task DisposeAsync_AutoRollsBack_WhenNotCommitted()
    {
        var uow = new UnitOfWork(_context);
        await uow.BeginAsync();

        // Dispose without commit — should auto-rollback without throwing
        var ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

        Assert.Null(ex);
    }

    /// <summary>Verifies that <c>DisposeAsync</c> does not throw when called after commit.</summary>
    [Fact]
    public async Task DisposeAsync_DoesNotThrow_WhenCalledAfterCommit()
    {
        var uow = new UnitOfWork(_context);
        await uow.BeginAsync();
        await uow.CommitAsync();

        var ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

        Assert.Null(ex);
    }

    /// <summary>Verifies that <c>DisposeAsync</c> does not throw when no transaction was started.</summary>
    [Fact]
    public async Task DisposeAsync_DoesNotThrow_WhenNoTransactionWasStarted()
    {
        var uow = new UnitOfWork(_context);

        var ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

        Assert.Null(ex);
    }
}
