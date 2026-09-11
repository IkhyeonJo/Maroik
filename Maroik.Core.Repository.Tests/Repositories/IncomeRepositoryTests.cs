using Maroik.Core.Domain.Finance;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfIncome = Maroik.Core.PostgreSQL.Models.Income;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="IncomeRepository"/> against a real PostgreSQL database (Testcontainers)
/// pre-loaded with the production <c>Init.sql</c> schema and seed data. <c>Income</c> carries the
/// composite FK <c>Income_fk_0</c> to <c>Asset(ProductName, AccountEmail)</c>, which the repository
/// eager-loads via <c>Include(x =&gt; x.Asset)</c> — so every test seeds the owning account and
/// asset first (<see cref="RepositoryTestBase.EnsureAssetsAsync"/>) and scopes queries to its own
/// unique account e-mail.
/// </summary>
public sealed class IncomeRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private IncomeRepository Sut => new(Context);

    private static EfIncome MakeIncome(string email, string assetName, string content = "Salary", decimal amount = 500m) => new()
    {
        AccountEmail = email,
        DepositMyAssetProductName = assetName,
        MainClass = "RegularIncome",
        SubClass = "LaborIncome",
        Content = content,
        Amount = amount,
        Note = "",
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params EfIncome[] incomes)
    {
        foreach (var group in incomes.GroupBy(i => i.AccountEmail!))
            await EnsureAssetsAsync(group.Key, [.. group.Select(i => i.DepositMyAssetProductName)]);

        await Context.Incomes.AddRangeAsync(incomes);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetByAccountEmailAsync ---------------------------------------------------

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns only incomes for the given e-mail.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsOnlyIncomesForGivenEmail()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        await SeedAsync(
            MakeIncome(alice, "SavingsA"),
            MakeIncome(alice, "SavingsB"),
            MakeIncome(bob, "BobSavings"));

        List<Income> result = await Sut.GetByAccountEmailAsync(alice, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal(alice, i.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns empty when the e-mail has no incomes.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsEmpty_WhenNoIncomesForEmail()
    {
        List<Income> result = await Sut.GetByAccountEmailAsync(UniqueEmail("nobody"), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- FindByEmailAndIdAsync -------------------------------------------------

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns the income when the e-mail and id match.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsIncome_WhenOwnedByEmail()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeIncome(alice, "Savings", amount: 900m);
        await SeedAsync(ef);

        Income? result = await Sut.FindByEmailAndIdAsync(alice, ef.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(900m, result.Amount.Amount);
    }

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns null for another account's income id.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsNull_WhenNotOwnedByEmail()
    {
        var ef = MakeIncome(UniqueEmail("bob"), "Savings");
        await SeedAsync(ef);

        Income? result = await Sut.FindByEmailAndIdAsync(UniqueEmail("alice"), ef.Id, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- FindByEmailAndIdForUpdateAsync (SELECT ... FOR UPDATE) --------------

    /// <summary>The row-locking lookup finds an existing income and returns null for a missing id.</summary>
    [Fact]
    public async Task FindByEmailAndIdForUpdateAsync_FindsIncome_OrReturnsNull()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeIncome(alice, "LockedSavings", amount: 640m);
        await SeedAsync(ef);

        Income? found = await Sut.FindByEmailAndIdForUpdateAsync(alice, ef.Id, TestContext.Current.CancellationToken);
        Income? missing = await Sut.FindByEmailAndIdForUpdateAsync(alice, long.MaxValue, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(640m, found.Amount.Amount);
        Assert.Null(missing);
    }

    /// <summary>
    /// The FOR UPDATE lookup re-reads through the included path, so the returned domain object still
    /// carries the monetary unit from the linked <c>Asset</c> — a bare <c>SELECT * … FOR UPDATE</c>
    /// row on its own would leave it blank.
    /// </summary>
    [Fact]
    public async Task FindByEmailAndIdForUpdateAsync_CarriesMonetaryUnit_FromAssetInclude()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeIncome(alice, "SavingsAccount", amount: 300m);
        await SeedAsync(ef); // EnsureAssetsAsync seeds the asset with MonetaryUnit "KRW"

        Income? found = await Sut.FindByEmailAndIdForUpdateAsync(alice, ef.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal("KRW", found.Amount.Currency);
    }

    /// <summary>
    /// Regression: the FOR UPDATE lookup re-reads through <c>FindByEmailAndIdAsync</c>-equivalent
    /// path, which must run <c>AsNoTracking()</c>. If this row is already tracked in the same
    /// <see cref="Context"/> from an earlier plain read, a tracking re-read would perform EF's
    /// identity resolution and hand back that pre-existing (stale) tracked instance instead of the
    /// row's just-locked, current committed values — silently defeating the FOR UPDATE lock.
    /// </summary>
    [Fact]
    public async Task FindByEmailAndIdForUpdateAsync_ReturnsFreshValues_WhenRowAlreadyTrackedInSameContext()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeIncome(alice, "StaleSavings", amount: 100m);
        await SeedAsync(ef);

        // Pre-track the row in this test's Context via a normal (tracking) read.
        Income? preRead = await Sut.FindByEmailAndIdAsync(alice, ef.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(preRead);

        // A concurrent writer commits a change to the same row via a separate connection.
        await using var writer = NewDbContext();
        await writer.Incomes.Where(i => i.Id == ef.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.Amount, 999m), TestContext.Current.CancellationToken);

        Income? locked = await Sut.FindByEmailAndIdForUpdateAsync(alice, ef.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(locked);
        Assert.Equal(999m, locked.Amount.Amount);
    }

    /// <summary>
    /// Reproduces the concurrent-edit race: two parallel transactions each read the same income,
    /// overlap, then write back an incremented amount. Without FOR UPDATE the second write is based
    /// on a stale read and clobbers the first (final 600); with it the reads serialize and both
    /// increments survive (final 700).
    /// </summary>
    [Fact]
    public async Task FindByEmailAndIdForUpdateAsync_SerializesConcurrentRecordEdits()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeIncome(alice, "RaceSavings", amount: 500m);
        await SeedAsync(ef);

        async Task BumpAsync()
        {
            await using var context = NewDbContext();
            var repo = new IncomeRepository(context);
            await using var tx = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

            Income? income = await repo.FindByEmailAndIdForUpdateAsync(alice, ef.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(income);

            await Task.Delay(300, TestContext.Current.CancellationToken);

            income.Update("RegularIncome", "LaborIncome", income.Content, income.Amount.Amount + 100m,
                income.Amount.Currency, income.DepositMyAssetProductName, income.Note);
            await repo.UpdateEntityAsync(income, TestContext.Current.CancellationToken);
            // The repository defers its flush while a transaction is open on the context; this test
            // drives the transaction directly, so it flushes the pending UPDATE itself before commit.
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(BumpAsync(), BumpAsync());

        Context.ChangeTracker.Clear();
        EfIncome? result = await Context.Incomes.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == ef.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(700m, result.Amount); // 500 + 100 + 100: no lost update
    }

    // -- CreateAsync --------------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> inserts an income row (DB generates its id).</summary>
    [Fact]
    public async Task CreateAsync_AddsIncomeToDatabase()
    {
        string alice = UniqueEmail("alice");
        await EnsureAssetsAsync(alice, "SavingsAccount");

        var income = Income.Reconstitute(
            id: 0, accountEmail: alice, mainClass: "RegularIncome", subClass: "LaborIncome",
            content: "Monthly salary", amount: 3000m, monetaryUnit: "",
            depositMyAssetProductName: "SavingsAccount", note: null,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Sut.CreateAsync(income, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfIncome? saved = await Context.Incomes.FirstOrDefaultAsync(
            i => i.AccountEmail == alice && i.Content == "Monthly salary", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(3000m, saved.Amount);
        Assert.True(saved.Id > 0);
    }

    /// <summary>PostgreSQL enforces <c>Income_fk_0</c> — an income pointing at a non-existent asset throws (InMemory did not).</summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenDepositAssetDoesNotExist()
    {
        string alice = UniqueEmail("alice");
        await EnsureAccountsAsync(alice);

        var income = Income.Reconstitute(
            id: 0, accountEmail: alice, mainClass: "RegularIncome", subClass: "LaborIncome",
            content: "Orphan", amount: 1m, monetaryUnit: "",
            depositMyAssetProductName: "NoSuchAsset", note: null,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.CreateAsync(income, TestContext.Current.CancellationToken));
    }

    // -- UpdateEntityAsync ----------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing income.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingIncome()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeIncome(alice, "SavingsAccount", amount: 500m);
        await SeedAsync(ef);

        var income = Income.Reconstitute(
            id: ef.Id, accountEmail: alice, mainClass: "RegularIncome", subClass: "BusinessIncome",
            content: "Updated Content", amount: 750m, monetaryUnit: "",
            depositMyAssetProductName: "SavingsAccount", note: null,
            created: ef.Created, updated: DateTime.UtcNow);

        await Sut.UpdateEntityAsync(income, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfIncome? updated = await Context.Incomes.FirstOrDefaultAsync(i => i.Id == ef.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(750m, updated.Amount);
        Assert.Equal("BusinessIncome", updated.SubClass);
    }

    // -- DeleteByIdAsync ----------------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes the income row.</summary>
    [Fact]
    public async Task DeleteByIdAsync_RemovesIncomeFromDatabase()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeIncome(alice, "SavingsAccount");
        await SeedAsync(ef);

        await Sut.DeleteByIdAsync(ef.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Null(await Context.Incomes.FirstOrDefaultAsync(i => i.Id == ef.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other incomes.</summary>
    [Fact]
    public async Task DeleteByIdAsync_DoesNotAffectOtherIncomes()
    {
        string alice = UniqueEmail("alice");
        var keep = MakeIncome(alice, "SavingsA");
        var remove = MakeIncome(alice, "SavingsB");
        await SeedAsync(keep, remove);

        await Sut.DeleteByIdAsync(remove.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        List<EfIncome> remaining = await Context.Incomes
            .Where(i => i.AccountEmail == alice).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(keep.Id, Assert.Single(remaining).Id);
    }
}
