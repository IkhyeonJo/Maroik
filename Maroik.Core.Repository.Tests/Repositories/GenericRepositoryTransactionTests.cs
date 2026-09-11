using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Calendar;
using Maroik.Core.Domain.Menu;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfCategory = Maroik.Core.PostgreSQL.Models.Category;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests the flush contract shared by every <see cref="GenericRepository{TDomain,TEntity}"/>:
/// a write is persisted immediately when no <see cref="UnitOfWork"/> transaction is open, but is
/// left pending — and flushed by <see cref="UnitOfWork.CommitAsync"/> as a single batch — while a
/// transaction is open. <see cref="CategoryRepository"/> is used as a representative concrete
/// repository (root table, no FKs).
/// </summary>
public sealed class GenericRepositoryTransactionTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private Category NewCategory() =>
        Category.Reconstitute(0, Unique("TxCat"), "Tx Category", "/icons/i.svg", "Tx", "Index", Role.User, 1);

    private async Task<bool> ExistsInSeparateConnectionAsync(string name)
    {
        await using var probe = NewDbContext();
        return await probe.Categories.AsNoTracking().AnyAsync(c => c.Name == name, TestContext.Current.CancellationToken);
    }

    /// <summary>Without a transaction, <c>CreateAsync</c> flushes right away (unchanged legacy behaviour).</summary>
    [Fact]
    public async Task CreateAsync_WithoutTransaction_FlushesImmediately()
    {
        var category = NewCategory();

        await new CategoryRepository(Context).CreateAsync(category, TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            Context.ChangeTracker.Entries<EfCategory>(),
            e => e.State == EntityState.Added);
        Assert.True(await ExistsInSeparateConnectionAsync(category.Name!));
    }

    /// <summary>Inside an open transaction the INSERT is deferred until <c>CommitAsync</c> flushes it.</summary>
    [Fact]
    public async Task CreateAsync_InsideTransaction_IsDeferredThenFlushedOnCommit()
    {
        await using var uow = new UnitOfWork(Context);
        var repo = new CategoryRepository(Context);
        var category = NewCategory();

        await uow.BeginAsync(TestContext.Current.CancellationToken);
        await repo.CreateAsync(category, TestContext.Current.CancellationToken);

        // Still pending — no SaveChanges was issued by the repository call.
        Assert.Contains(
            Context.ChangeTracker.Entries<EfCategory>(),
            e => e.State == EntityState.Added && ((EfCategory)e.Entity).Name == category.Name);

        await uow.CommitAsync(TestContext.Current.CancellationToken);

        Assert.DoesNotContain(
            Context.ChangeTracker.Entries<EfCategory>(),
            e => e.State == EntityState.Added);
        Assert.True(await ExistsInSeparateConnectionAsync(category.Name!));
    }

    /// <summary>A deferred write is discarded when the transaction is rolled back.</summary>
    [Fact]
    public async Task CreateAsync_InsideTransaction_RolledBack_IsNotPersisted()
    {
        await using var uow = new UnitOfWork(Context);
        var repo = new CategoryRepository(Context);
        var category = NewCategory();

        await uow.BeginAsync(TestContext.Current.CancellationToken);
        await repo.CreateAsync(category, TestContext.Current.CancellationToken);
        await uow.RollbackAsync(TestContext.Current.CancellationToken);

        Assert.False(await ExistsInSeparateConnectionAsync(category.Name!));
    }

    /// <summary>
    /// Regression: <c>DeleteWhereAsync</c> used to compute its delete set purely from a DB query, so
    /// a row created earlier in the same still-open transaction via <c>CreateAsync</c> — whose
    /// INSERT is deferred, per this class's flush contract — was invisible to it and got inserted
    /// anyway by the eventual <c>CommitAsync</c>, silently ignoring the caller's delete.
    /// <see cref="OtherCalendar"/> is used because its (AccountEmail, CalendarId) primary key is
    /// caller-supplied, so the delete predicate does not need a DB-generated id that only exists
    /// after the deferred INSERT is flushed.
    /// </summary>
    [Fact]
    public async Task DeleteWhereAsync_InsideTransaction_RemovesRowCreatedEarlierInSameTransaction()
    {
        long calendarId = await InsertCalendarAsync();
        string email = UniqueEmail("subscriber");
        await EnsureAccountsAsync(email);
        var link = OtherCalendar.Reconstitute(email, calendarId);

        await using var uow = new UnitOfWork(Context);
        var repo = new OtherCalendarRepository(Context);

        await uow.BeginAsync(TestContext.Current.CancellationToken);
        await repo.CreateAsync(link, TestContext.Current.CancellationToken);
        await repo.DeleteByAccountEmailAndCalendarIdAsync(email, calendarId, TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        Assert.False(await Context.OtherCalendars.AsNoTracking().AnyAsync(
            o => o.AccountEmail == email && o.CalendarId == calendarId, TestContext.Current.CancellationToken));
    }
}
