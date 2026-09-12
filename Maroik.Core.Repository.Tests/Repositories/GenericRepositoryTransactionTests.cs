using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Calendar;
using Maroik.Core.Domain.Menu;
using Maroik.Core.PostgreSQL.Data;
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
    /// Regression: <c>QueryAsync</c> (used here via <c>OtherCalendarRepository.GetByAccountEmailAsync</c>)
    /// used to translate its predicate straight into SQL and run it against the database only, so a row
    /// created earlier in the same still-open transaction -- whose INSERT is deferred -- was invisible
    /// to it even though it already exists for every practical purpose within this unit of work.
    /// </summary>
    [Fact]
    public async Task QueryAsync_InsideTransaction_SeesRowCreatedEarlierInSameTransaction()
    {
        long calendarId = await InsertCalendarAsync();
        string email = UniqueEmail("subscriber-query");
        await EnsureAccountsAsync(email);
        var link = OtherCalendar.Reconstitute(email, calendarId);

        await using var uow = new UnitOfWork(Context);
        var repo = new OtherCalendarRepository(Context);

        await uow.BeginAsync(TestContext.Current.CancellationToken);
        await repo.CreateAsync(link, TestContext.Current.CancellationToken);
        List<OtherCalendar> subscriptions = await repo.GetByAccountEmailAsync(email, TestContext.Current.CancellationToken);

        Assert.Contains(subscriptions, s => s.CalendarId == calendarId);
    }

    /// <summary>
    /// Regression: <c>QueryFirstAsync</c> (used here via <c>AccountRepository.FindByEmailAsync</c>) had
    /// the same DB-only gap as <c>QueryAsync</c> above -- a row created earlier in the same still-open
    /// transaction was invisible to it because its INSERT is deferred until <c>CommitAsync</c>.
    /// </summary>
    [Fact]
    public async Task QueryFirstAsync_InsideTransaction_SeesRowCreatedEarlierInSameTransaction()
    {
        string email = UniqueEmail("first-query");
        var account = Account.Reconstitute(
            email: email, hashedPassword: "$2a$13$placeholder", nickname: "nick-" + Token,
            avatarImagePath: null, role: Role.User, timeZoneIanaId: "UTC", defaultMonetaryUnit: null,
            locked: false, loginAttempt: 0, emailConfirmed: true, agreedServiceTerms: true,
            registrationToken: null, resetPasswordToken: null, created: DateTime.UtcNow, updated: DateTime.UtcNow,
            message: null, deleted: false, securityStamp: "stamp", mustChangePassword: false);

        await using var uow = new UnitOfWork(Context);
        var repo = new AccountRepository(Context);

        await uow.BeginAsync(TestContext.Current.CancellationToken);
        await repo.CreateAsync(account, TestContext.Current.CancellationToken);
        Account? found = await repo.FindByEmailAsync(email, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(email, found!.Email.Value);
    }

    /// <summary>Test-only subclass exposing a <c>DeleteWhereAsync</c> filtered on <c>Name</c> (a mutable,
    /// non-key column) so the Modified-row extension below can be exercised through it.</summary>
    private sealed class NameFilterableCategoryRepository(ApplicationDbContext context) : CategoryRepository(context)
    {
        public Task DeleteByNameAsync(string name, CancellationToken ct) => DeleteWhereAsync(e => e.Name == name, ct);
    }

    /// <summary>
    /// Regression: <c>DeleteWhereAsync</c>'s delete-set query is deliberately <c>AsNoTracking</c> (see
    /// its own comment), so it cannot see a row renamed -- but not yet flushed -- earlier in the same
    /// transaction; without the Modified-row reconciliation, a predicate matching only the row's new,
    /// unflushed name would silently delete nothing and the pending rename would still commit.
    /// </summary>
    [Fact]
    public async Task DeleteWhereAsync_InsideTransaction_RemovesRowRenamedEarlierInSameTransactionToMatchPredicate()
    {
        string originalName = Unique("OrigCat");
        string newName = Unique("RenamedCat");
        var repo = new NameFilterableCategoryRepository(Context);
        var category = Category.Reconstitute(0, originalName, "Display", "/icons/i.svg", "Ctrl", "Index", Role.User, 1);
        await repo.CreateAsync(category, TestContext.Current.CancellationToken);
        long id = await Context.Categories.AsNoTracking()
            .Where(c => c.Name == originalName)
            .Select(c => c.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        await using var uow = new UnitOfWork(Context);
        await uow.BeginAsync(TestContext.Current.CancellationToken);
        var renamed = Category.Reconstitute(id, newName, "Display", "/icons/i.svg", "Ctrl", "Index", Role.User, 1);
        await repo.UpdateEntityAsync(renamed, TestContext.Current.CancellationToken);
        await repo.DeleteByNameAsync(newName, TestContext.Current.CancellationToken);
        await uow.CommitAsync(TestContext.Current.CancellationToken);

        Assert.False(await ExistsInSeparateConnectionAsync(originalName));
        Assert.False(await ExistsInSeparateConnectionAsync(newName));
    }
}
