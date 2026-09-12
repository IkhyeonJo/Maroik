using Maroik.Core.Domain.Finance;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="FixedExpenditureRepository"/> against a real PostgreSQL database
/// (Testcontainers) pre-loaded with the production <c>Init.sql</c> schema and seed data.
/// <c>FixedExpenditure</c> carries the composite FKs <c>FixedExpenditure_fk_0</c> on
/// <c>(PaymentMethod, AccountEmail)</c> and <c>FixedExpenditure_fk_1</c> on
/// <c>(MyDepositAsset, AccountEmail)</c>. Includes <c>UpdateMyDepositAssetWithProductNameAsync</c>
/// (<c>ExecuteUpdateAsync</c>), which the EF Core InMemory provider could not run.
/// </summary>
public sealed class FixedExpenditureRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private FixedExpenditureRepository Sut => new(Context);

    private static EfFixedExpenditure MakeFixedExpenditure(
        string email, string content, string paymentMethod = "Wallet", string? myDepositAsset = "Wallet",
        decimal amount = 50m) => new()
    {
        AccountEmail = email,
        MainClass = "ConsumerSpending",
        SubClass = "Tax",
        Content = content,
        Amount = amount,
        PaymentMethod = paymentMethod,
        MyDepositAsset = myDepositAsset,
        DepositMonth = 1,
        DepositDay = 25,
        MaturityDate = DateTime.UtcNow.AddYears(1),
        Note = "",
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        Unpunctuality = false
    };

    private async Task SeedAsync(params EfFixedExpenditure[] items)
    {
        foreach (var group in items.GroupBy(e => e.AccountEmail!))
            await EnsureAssetsAsync(group.Key, [.. group.SelectMany(e => new[] { e.PaymentMethod, e.MyDepositAsset })]);

        await Context.FixedExpenditures.AddRangeAsync(items);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetByAccountEmailAsync --------------------------------------------------

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns only items for the given e-mail.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsOnlyItemsForGivenEmail()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        await SeedAsync(
            MakeFixedExpenditure(alice, "Netflix"),
            MakeFixedExpenditure(alice, "Insurance"),
            MakeFixedExpenditure(bob, "Gym"));

        List<FixedExpenditure> result = await Sut.GetByAccountEmailAsync(alice, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, fe => Assert.Equal(alice, fe.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns empty when the e-mail has no items.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsEmpty_WhenNoItemsForEmail()
    {
        List<FixedExpenditure> result = await Sut.GetByAccountEmailAsync(UniqueEmail("nobody"), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- FindByEmailAndIdAsync -------------------------------------------------

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns the item when owned by the e-mail.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsItem_WhenOwnedByEmail()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeFixedExpenditure(alice, "Netflix", amount: 17m);
        await SeedAsync(ef);

        FixedExpenditure? result = await Sut.FindByEmailAndIdAsync(alice, ef.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(17m, result.Amount.Amount);
    }

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns null for another account's id.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsNull_WhenNotOwnedByEmail()
    {
        var ef = MakeFixedExpenditure(UniqueEmail("bob"), "Gym");
        await SeedAsync(ef);

        FixedExpenditure? result = await Sut.FindByEmailAndIdAsync(UniqueEmail("alice"), ef.Id, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- CreateAsync ------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> inserts a fixed-expenditure row.</summary>
    [Fact]
    public async Task CreateAsync_AddsItemToDatabase()
    {
        string alice = UniqueEmail("alice");
        await EnsureAssetsAsync(alice, "BankAccount");

        var fixedExpenditure = FixedExpenditure.Reconstitute(
            id: 0, accountEmail: alice, mainClass: "ConsumerSpending", subClass: "Tax",
            content: "Property tax", amount: 200m, monetaryUnit: "",
            paymentMethod: "BankAccount", myDepositAsset: "BankAccount", depositMonth: 3, depositDay: 31,
            maturityDate: DateTime.UtcNow.AddYears(1), note: null, unpunctuality: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Sut.CreateAsync(fixedExpenditure, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfFixedExpenditure? saved = await Context.FixedExpenditures.FirstOrDefaultAsync(
            fe => fe.AccountEmail == alice && fe.Content == "Property tax", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(200m, saved.Amount);
        Assert.Equal((short)3, saved.DepositMonth);
        Assert.Equal((short)31, saved.DepositDay);
    }

    // -- UpdateEntityAsync ------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing item.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingItem()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeFixedExpenditure(alice, "Netflix", amount: 50m);
        await SeedAsync(ef);

        var fixedExpenditure = FixedExpenditure.Reconstitute(
            id: ef.Id, accountEmail: alice, mainClass: "ConsumerSpending", subClass: "Tax",
            content: "Netflix Premium", amount: 75m, monetaryUnit: "",
            paymentMethod: "Wallet", myDepositAsset: "Wallet", depositMonth: 1, depositDay: 25,
            maturityDate: ef.MaturityDate, note: null, unpunctuality: true,
            created: ef.Created, updated: DateTime.UtcNow);

        await Sut.UpdateEntityAsync(fixedExpenditure, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfFixedExpenditure? updated = await Context.FixedExpenditures.FirstOrDefaultAsync(fe => fe.Id == ef.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(75m, updated.Amount);
        Assert.Equal("Netflix Premium", updated.Content);
        Assert.True(updated.Unpunctuality);
    }

    // -- DeleteByIdAsync ------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes the item row.</summary>
    [Fact]
    public async Task DeleteByIdAsync_RemovesItemFromDatabase()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeFixedExpenditure(alice, "Spotify");
        await SeedAsync(ef);

        await Sut.DeleteByIdAsync(ef.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Null(await Context.FixedExpenditures.FirstOrDefaultAsync(fe => fe.Id == ef.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other items.</summary>
    [Fact]
    public async Task DeleteByIdAsync_DoesNotAffectOtherItems()
    {
        string alice = UniqueEmail("alice");
        var keep = MakeFixedExpenditure(alice, "Netflix");
        var remove = MakeFixedExpenditure(alice, "Spotify");
        await SeedAsync(keep, remove);

        await Sut.DeleteByIdAsync(remove.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        List<EfFixedExpenditure> remaining = await Context.FixedExpenditures
            .Where(fe => fe.AccountEmail == alice).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(keep.Id, Assert.Single(remaining).Id);
    }
}
