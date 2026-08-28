using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="UnitOfWork"/> that need a real, transaction-capable
/// provider (the EF Core InMemory provider used by <see cref="UnitOfWorkTests"/> cannot begin
/// a transaction). Covers the re-entrancy guard and the sequential-reuse path.
/// </summary>
[Trait("Category", "Integration")]
public class UnitOfWorkIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17").Build();
    private ApplicationDbContext _context = null!;
    private UnitOfWork _sut = null!;

    /// <summary>Starts the PostgreSQL container and creates the schema before each test.</summary>
    public async ValueTask InitializeAsync()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new UnitOfWork(_context);
    }

    /// <summary>Disposes the context and stops the container after each test.</summary>
    public async ValueTask DisposeAsync()
    {
        await _sut.DisposeAsync();
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    /// <summary>A second <c>BeginAsync</c> while a transaction is still live throws instead of discarding the first.</summary>
    [Fact]
    public async Task BeginAsync_WhileTransactionActive_Throws()
    {
        await _sut.BeginAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.BeginAsync(TestContext.Current.CancellationToken));
    }

    /// <summary>After a commit the slot is cleared, so the same instance can run a second Begin→Commit cycle.</summary>
    [Fact]
    public async Task BeginAsync_AfterCommit_CanStartAnotherTransaction()
    {
        await _sut.BeginAsync(TestContext.Current.CancellationToken);
        await _sut.CommitAsync(TestContext.Current.CancellationToken);

        var ex = await Record.ExceptionAsync(async () =>
        {
            await _sut.BeginAsync(TestContext.Current.CancellationToken);
            await _sut.CommitAsync(TestContext.Current.CancellationToken);
        });

        Assert.Null(ex);
    }

    /// <summary>Calling <c>RollbackAsync</c> after a commit is a harmless no-op (the catch-block pattern in services).</summary>
    [Fact]
    public async Task RollbackAsync_AfterCommit_DoesNotThrow()
    {
        await _sut.BeginAsync(TestContext.Current.CancellationToken);
        await _sut.CommitAsync(TestContext.Current.CancellationToken);

        var ex = await Record.ExceptionAsync(() => _sut.RollbackAsync(TestContext.Current.CancellationToken));

        Assert.Null(ex);
    }
}
