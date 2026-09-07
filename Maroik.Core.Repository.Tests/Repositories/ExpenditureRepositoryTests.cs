using Maroik.Core.Domain.Finance;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfExpenditure = Maroik.Core.PostgreSQL.Models.Expenditure;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="ExpenditureRepository"/> against a real PostgreSQL database (Testcontainers)
/// pre-loaded with the production <c>Init.sql</c> schema and seed data. <c>Expenditure</c> carries
/// two composite FKs to <c>Asset</c>: <c>Expenditure_fk_0</c> on <c>(PaymentMethod, AccountEmail)</c>
/// and <c>Expenditure_fk_1</c> on <c>(MyDepositAsset, AccountEmail)</c> (NULL when the entry is not
/// a transfer). Includes <c>UpdateMyDepositAssetWithProductNameAsync</c>, which uses
/// <c>ExecuteUpdateAsync</c> — a translation the EF Core InMemory provider could not run.
/// </summary>
public sealed class ExpenditureRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private ExpenditureRepository Sut => new(Context);

    private static EfExpenditure MakeExpenditure(
        string email, string content, string paymentMethod = "Wallet", string? myDepositAsset = "Wallet",
        decimal amount = 100m) => new()
    {
        AccountEmail = email,
        MainClass = "ConsumerSpending",
        SubClass = "MealOrEatOutExpenses",
        Content = content,
        Amount = amount,
        PaymentMethod = paymentMethod,
        MyDepositAsset = myDepositAsset,
        Note = "",
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params EfExpenditure[] expenditures)
    {
        foreach (var group in expenditures.GroupBy(e => e.AccountEmail!))
            await EnsureAssetsAsync(group.Key, [.. group.SelectMany(e => new[] { e.PaymentMethod, e.MyDepositAsset })]);

        await Context.Expenditures.AddRangeAsync(expenditures);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetByAccountEmailAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns only expenditures for the given e-mail.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsOnlyExpendituresForGivenEmail()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        await SeedAsync(
            MakeExpenditure(alice, "Lunch"),
            MakeExpenditure(alice, "Dinner"),
            MakeExpenditure(bob, "Coffee"));

        List<Expenditure> result = await Sut.GetByAccountEmailAsync(alice, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(alice, e.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns empty when the e-mail has no expenditures.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsEmpty_WhenNoExpendituresForEmail()
    {
        List<Expenditure> result = await Sut.GetByAccountEmailAsync(UniqueEmail("nobody"), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    /// <summary>
    /// Regression: a non-transfer expenditure (<c>MyDepositAsset</c> NULL) still resolves its
    /// currency from the <c>PaymentMethod</c> asset via <c>AssetNavigation</c> — the mapping used
    /// to join the wrong navigation and return an empty <c>MonetaryUnit</c> for the common case.
    /// </summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ResolvesCurrency_FromPaymentMethodAsset_ForNonTransferExpenditure()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeExpenditure(alice, "Lunch", paymentMethod: "Wallet", myDepositAsset: null));

        List<Expenditure> result = await Sut.GetByAccountEmailAsync(alice, TestContext.Current.CancellationToken);

        Assert.Equal("KRW", Assert.Single(result).Amount.Currency);
    }

    // -- FindByEmailAndIdAsync -------------------------------------------------

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns the expenditure when owned by the e-mail.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsExpenditure_WhenOwnedByEmail()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeExpenditure(alice, "Lunch", amount: 42m);
        await SeedAsync(ef);

        Expenditure? result = await Sut.FindByEmailAndIdAsync(alice, ef.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(42m, result.Amount.Amount);
    }

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns null for another account's id.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsNull_WhenNotOwnedByEmail()
    {
        var ef = MakeExpenditure(UniqueEmail("bob"), "Coffee");
        await SeedAsync(ef);

        Expenditure? result = await Sut.FindByEmailAndIdAsync(UniqueEmail("alice"), ef.Id, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- FindByEmailAndIdForUpdateAsync (SELECT ... FOR UPDATE) --------------

    /// <summary>The row-locking lookup finds an existing expenditure and returns null for a missing id.</summary>
    [Fact]
    public async Task FindByEmailAndIdForUpdateAsync_FindsExpenditure_OrReturnsNull()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeExpenditure(alice, "Lunch", amount: 42m);
        await SeedAsync(ef);

        Expenditure? found = await Sut.FindByEmailAndIdForUpdateAsync(alice, ef.Id, TestContext.Current.CancellationToken);
        Expenditure? missing = await Sut.FindByEmailAndIdForUpdateAsync(alice, long.MaxValue, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(42m, found.Amount.Amount);
        Assert.Null(missing);
    }

    /// <summary>
    /// The FOR UPDATE lookup re-reads through the included path, so the returned domain object still
    /// carries the monetary unit from the linked payment <c>Asset</c> — a bare
    /// <c>SELECT * … FOR UPDATE</c> row on its own would leave it blank.
    /// </summary>
    [Fact]
    public async Task FindByEmailAndIdForUpdateAsync_CarriesMonetaryUnit_FromAssetInclude()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeExpenditure(alice, "Lunch", amount: 20m);
        await SeedAsync(ef); // EnsureAssetsAsync seeds the asset with MonetaryUnit "KRW"

        Expenditure? found = await Sut.FindByEmailAndIdForUpdateAsync(alice, ef.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal("KRW", found.Amount.Currency);
    }

    /// <summary>
    /// Reproduces the concurrent-edit race: two parallel transactions each read the same
    /// expenditure, overlap, then write back an incremented amount. Without FOR UPDATE the second
    /// write is based on a stale read and clobbers the first (final 300); with it the reads
    /// serialize and both increments survive (final 500).
    /// </summary>
    [Fact]
    public async Task FindByEmailAndIdForUpdateAsync_SerializesConcurrentRecordEdits()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeExpenditure(alice, "RaceSpend", amount: 300m);
        await SeedAsync(ef);

        async Task BumpAsync()
        {
            await using var context = NewDbContext();
            var repo = new ExpenditureRepository(context);
            await using var tx = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

            Expenditure? expenditure = await repo.FindByEmailAndIdForUpdateAsync(alice, ef.Id, TestContext.Current.CancellationToken);
            Assert.NotNull(expenditure);

            await Task.Delay(300, TestContext.Current.CancellationToken);

            expenditure.Update("ConsumerSpending", "MealOrEatOutExpenses", expenditure.Content,
                expenditure.Amount.Amount + 100m, expenditure.Amount.Currency,
                expenditure.PaymentMethod, expenditure.MyDepositAsset, expenditure.Note);
            await repo.UpdateEntityAsync(expenditure, TestContext.Current.CancellationToken);
            // The repository defers its flush while a transaction is open on the context; this test
            // drives the transaction directly, so it flushes the pending UPDATE itself before commit.
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(BumpAsync(), BumpAsync());

        Context.ChangeTracker.Clear();
        EfExpenditure? result = await Context.Expenditures.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == ef.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(500m, result.Amount); // 300 + 100 + 100: no lost update
    }

    // -- CreateAsync --------------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> inserts an expenditure row.</summary>
    [Fact]
    public async Task CreateAsync_AddsExpenditureToDatabase()
    {
        string alice = UniqueEmail("alice");
        await EnsureAssetsAsync(alice, "CreditCard");

        var expenditure = Expenditure.Reconstitute(
            id: 0, accountEmail: alice, mainClass: "ConsumerSpending", subClass: "MealOrEatOutExpenses",
            content: "Team lunch", amount: 250m, monetaryUnit: "",
            paymentMethod: "CreditCard", myDepositAsset: "CreditCard", note: null,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Sut.CreateAsync(expenditure, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfExpenditure? saved = await Context.Expenditures.FirstOrDefaultAsync(
            e => e.AccountEmail == alice && e.Content == "Team lunch", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(250m, saved.Amount);
    }

    /// <summary>PostgreSQL enforces <c>Expenditure_fk_0</c> — an unknown <c>PaymentMethod</c> asset throws (InMemory did not).</summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenPaymentMethodAssetDoesNotExist()
    {
        string alice = UniqueEmail("alice");
        await EnsureAccountsAsync(alice);

        var expenditure = Expenditure.Reconstitute(
            id: 0, accountEmail: alice, mainClass: "ConsumerSpending", subClass: "MealOrEatOutExpenses",
            content: "Orphan", amount: 1m, monetaryUnit: "",
            paymentMethod: "NoSuchAsset", myDepositAsset: null, note: null,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.CreateAsync(expenditure, TestContext.Current.CancellationToken));
    }

    // -- UpdateEntityAsync -----------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing expenditure.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingExpenditure()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeExpenditure(alice, "Lunch", amount: 100m);
        await SeedAsync(ef);

        var expenditure = Expenditure.Reconstitute(
            id: ef.Id, accountEmail: alice, mainClass: "ConsumerSpending", subClass: "MealOrEatOutExpenses",
            content: "Updated Lunch", amount: 150m, monetaryUnit: "",
            paymentMethod: "Wallet", myDepositAsset: "Wallet", note: null,
            created: ef.Created, updated: DateTime.UtcNow);

        await Sut.UpdateEntityAsync(expenditure, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfExpenditure? updated = await Context.Expenditures.FirstOrDefaultAsync(e => e.Id == ef.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(150m, updated.Amount);
        Assert.Equal("Updated Lunch", updated.Content);
    }

    // -- DeleteByIdAsync -----------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes the expenditure row.</summary>
    [Fact]
    public async Task DeleteByIdAsync_RemovesExpenditureFromDatabase()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeExpenditure(alice, "Taxi");
        await SeedAsync(ef);

        await Sut.DeleteByIdAsync(ef.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Null(await Context.Expenditures.FirstOrDefaultAsync(e => e.Id == ef.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other expenditures.</summary>
    [Fact]
    public async Task DeleteByIdAsync_DoesNotAffectOtherExpenditures()
    {
        string alice = UniqueEmail("alice");
        var keep = MakeExpenditure(alice, "Lunch");
        var remove = MakeExpenditure(alice, "Dinner");
        await SeedAsync(keep, remove);

        await Sut.DeleteByIdAsync(remove.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        List<EfExpenditure> remaining = await Context.Expenditures
            .Where(e => e.AccountEmail == alice).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(keep.Id, Assert.Single(remaining).Id);
    }

    // -- UpdateMyDepositAssetWithProductNameAsync (ExecuteUpdateAsync) --------

    /// <summary>
    /// Verifies that renaming an asset cascades to every expenditure that references it as
    /// <c>MyDepositAsset</c> for that account, and returns the row count. The target asset name is
    /// created first, mirroring <c>AssetService</c>'s ordering (the <c>Asset</c> row is renamed
    /// before this cascade runs, so <c>Expenditure_fk_1</c> is satisfied at commit).
    /// </summary>
    [Fact]
    public async Task UpdateMyDepositAssetWithProductNameAsync_RenamesMatchingRows()
    {
        string alice = UniqueEmail("alice");
        var e1 = MakeExpenditure(alice, "Transfer 1", myDepositAsset: "OldName");
        var e2 = MakeExpenditure(alice, "Transfer 2", myDepositAsset: "OldName");
        var e3 = MakeExpenditure(alice, "Unrelated", myDepositAsset: "OtherAsset");
        await SeedAsync(e1, e2, e3);
        await EnsureAssetsAsync(alice, "NewName");

        int updated = await Sut.UpdateMyDepositAssetWithProductNameAsync(alice, "OldName", "NewName", TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Equal(2, updated);
        Assert.Equal("NewName", (await Context.Expenditures.FirstAsync(e => e.Id == e1.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
        Assert.Equal("NewName", (await Context.Expenditures.FirstAsync(e => e.Id == e2.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
        Assert.Equal("OtherAsset", (await Context.Expenditures.FirstAsync(e => e.Id == e3.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
    }

    /// <summary>Verifies that the cascade is scoped to the given account.</summary>
    [Fact]
    public async Task UpdateMyDepositAssetWithProductNameAsync_DoesNotAffectOtherAccounts()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        var mine = MakeExpenditure(alice, "Mine", paymentMethod: "AliceWallet", myDepositAsset: "SharedName");
        var theirs = MakeExpenditure(bob, "Not mine", paymentMethod: "BobWallet", myDepositAsset: "SharedName");
        await SeedAsync(mine, theirs);
        await EnsureAssetsAsync(alice, "Renamed");

        int updated = await Sut.UpdateMyDepositAssetWithProductNameAsync(alice, "SharedName", "Renamed", TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Equal(1, updated);
        Assert.Equal("Renamed", (await Context.Expenditures.FirstAsync(e => e.Id == mine.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
        Assert.Equal("SharedName", (await Context.Expenditures.FirstAsync(e => e.Id == theirs.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
    }

    /// <summary>Verifies that renaming a name with no matching rows affects zero rows.</summary>
    [Fact]
    public async Task UpdateMyDepositAssetWithProductNameAsync_ReturnsZero_WhenNoMatch()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeExpenditure(alice, "Only", myDepositAsset: "SomeAsset"));

        int updated = await Sut.UpdateMyDepositAssetWithProductNameAsync(alice, "DoesNotExist", "NewName", TestContext.Current.CancellationToken);

        Assert.Equal(0, updated);
    }
}
