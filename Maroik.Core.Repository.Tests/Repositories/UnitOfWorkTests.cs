using Maroik.Core.Repository.Repositories;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="UnitOfWork"/> against a real, transaction-capable PostgreSQL provider
/// (Testcontainers). Covers both the null-guard paths (<c>if (_transaction == null) return;</c>)
/// and — now that a real transaction can actually begin, unlike under the removed EF Core InMemory
/// provider — the re-entrancy guard and the sequential-reuse path.
/// </summary>
public sealed class UnitOfWorkTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private UnitOfWork NewUnitOfWork() => new(NewDbContext());

    // -- Null-guard paths (no BeginAsync) -------------------------------------

    /// <summary>Verifies that <c>CommitAsync</c> does not throw when begin was not called.</summary>
    [Fact]
    public async Task CommitAsync_DoesNotThrow_WhenBeginWasNotCalled()
    {
        await using var uow = NewUnitOfWork();

        Exception? ex = await Record.ExceptionAsync(() => uow.CommitAsync(TestContext.Current.CancellationToken));

        Assert.Null(ex);
    }

    /// <summary>Verifies that <c>RollbackAsync</c> does not throw when begin was not called.</summary>
    [Fact]
    public async Task RollbackAsync_DoesNotThrow_WhenBeginWasNotCalled()
    {
        await using var uow = NewUnitOfWork();

        Exception? ex = await Record.ExceptionAsync(() => uow.RollbackAsync(TestContext.Current.CancellationToken));

        Assert.Null(ex);
    }

    /// <summary>Verifies that <c>DisposeAsync</c> does not throw when no transaction was started.</summary>
    [Fact]
    public async Task DisposeAsync_DoesNotThrow_WhenNoTransactionWasStarted()
    {
        var uow = NewUnitOfWork();

        Exception? ex = await Record.ExceptionAsync(async () => await uow.DisposeAsync());

        Assert.Null(ex);
    }

    // -- Real-transaction paths --------------------------------------------------

    /// <summary>A second <c>BeginAsync</c> while a transaction is still live throws instead of discarding the first.</summary>
    [Fact]
    public async Task BeginAsync_WhileTransactionActive_Throws()
    {
        await using var uow = NewUnitOfWork();
        await uow.BeginAsync(TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => uow.BeginAsync(TestContext.Current.CancellationToken));

        await uow.RollbackAsync(TestContext.Current.CancellationToken);
    }

    /// <summary>After a commit the slot is cleared, so the same instance can run a second Begin→Commit cycle.</summary>
    [Fact]
    public async Task BeginAsync_AfterCommit_CanStartAnotherTransaction()
    {
        await using var uow = NewUnitOfWork();
        await uow.BeginAsync(TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        Exception? ex = await Record.ExceptionAsync(async () =>
        {
            await uow.BeginAsync(TestContext.Current.CancellationToken);
            await uow.CommitAsync(TestContext.Current.CancellationToken);
        });

        Assert.Null(ex);
    }

    /// <summary>Calling <c>RollbackAsync</c> after a commit is a harmless no-op (the catch-block pattern in services).</summary>
    [Fact]
    public async Task RollbackAsync_AfterCommit_DoesNotThrow()
    {
        await using var uow = NewUnitOfWork();
        await uow.BeginAsync(TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        Exception? ex = await Record.ExceptionAsync(() => uow.RollbackAsync(TestContext.Current.CancellationToken));

        Assert.Null(ex);
    }
}
