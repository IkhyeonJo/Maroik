using Maroik.Core.Domain.Finance;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfFixedIncome = Maroik.Core.PostgreSQL.Models.FixedIncome;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="FixedIncomeRepository"/> against a real PostgreSQL database (Testcontainers)
/// pre-loaded with the production <c>Init.sql</c> schema and seed data. <c>FixedIncome</c> carries
/// the composite FK <c>FixedIncome_fk_0</c> to <c>Asset(ProductName, AccountEmail)</c> which the
/// repository eager-loads; each test seeds the owning account and asset and scopes to its own
/// unique account e-mail.
/// </summary>
public sealed class FixedIncomeRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private FixedIncomeRepository Sut => new(Context);

    private static EfFixedIncome MakeFixedIncome(string email, string content, string asset = "BankAccount", decimal amount = 3000m) => new()
    {
        AccountEmail = email,
        MainClass = "RegularIncome",
        SubClass = "LaborIncome",
        Content = content,
        Amount = amount,
        DepositMyAssetProductName = asset,
        DepositMonth = 1,
        DepositDay = 25,
        MaturityDate = DateTime.UtcNow.AddYears(1),
        Note = "",
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        Unpunctuality = false
    };

    private async Task SeedAsync(params EfFixedIncome[] items)
    {
        foreach (var group in items.GroupBy(i => i.AccountEmail!))
            await EnsureAssetsAsync(group.Key, [.. group.Select(i => i.DepositMyAssetProductName)]);

        await Context.FixedIncomes.AddRangeAsync(items);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetByAccountEmailAsync ---------------------------------------------------

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns only items for the given e-mail.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsOnlyItemsForGivenEmail()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        await SeedAsync(
            MakeFixedIncome(alice, "Salary"),
            MakeFixedIncome(alice, "Side job"),
            MakeFixedIncome(bob, "Bob salary"));

        List<FixedIncome> result = await Sut.GetByAccountEmailAsync(alice, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, fi => Assert.Equal(alice, fi.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns empty when the e-mail has no items.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsEmpty_WhenNoItemsForEmail()
    {
        List<FixedIncome> result = await Sut.GetByAccountEmailAsync(UniqueEmail("nobody"), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- FindByEmailAndIdAsync -------------------------------------------------

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns the item when owned by the e-mail.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsItem_WhenOwnedByEmail()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeFixedIncome(alice, "Salary", amount: 4200m);
        await SeedAsync(ef);

        FixedIncome? result = await Sut.FindByEmailAndIdAsync(alice, ef.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(4200m, result.Amount.Amount);
    }

    /// <summary>Verifies that <c>FindByEmailAndIdAsync</c> returns null for another account's id.</summary>
    [Fact]
    public async Task FindByEmailAndIdAsync_ReturnsNull_WhenNotOwnedByEmail()
    {
        var ef = MakeFixedIncome(UniqueEmail("bob"), "Salary");
        await SeedAsync(ef);

        FixedIncome? result = await Sut.FindByEmailAndIdAsync(UniqueEmail("alice"), ef.Id, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- CreateAsync --------------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> inserts a fixed-income row.</summary>
    [Fact]
    public async Task CreateAsync_AddsItemToDatabase()
    {
        string alice = UniqueEmail("alice");
        await EnsureAssetsAsync(alice, "BankAccount");

        var fixedIncome = FixedIncome.Reconstitute(
            id: 0, accountEmail: alice, mainClass: "RegularIncome", subClass: "LaborIncome",
            content: "Monthly salary", amount: 5000m, monetaryUnit: "",
            depositMyAssetProductName: "BankAccount", depositMonth: 1, depositDay: 25,
            maturityDate: DateTime.UtcNow.AddYears(2), note: null, unpunctuality: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Sut.CreateAsync(fixedIncome, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfFixedIncome? saved = await Context.FixedIncomes.FirstOrDefaultAsync(
            fi => fi.AccountEmail == alice && fi.Content == "Monthly salary", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(5000m, saved.Amount);
        Assert.Equal((short)25, saved.DepositDay);
    }

    // -- UpdateEntityAsync -----------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing item.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingItem()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeFixedIncome(alice, "Salary", amount: 3000m);
        await SeedAsync(ef);

        var fixedIncome = FixedIncome.Reconstitute(
            id: ef.Id, accountEmail: alice, mainClass: "RegularIncome", subClass: "LaborIncome",
            content: "Salary (Raise)", amount: 3500m, monetaryUnit: "",
            depositMyAssetProductName: "BankAccount", depositMonth: 1, depositDay: 25,
            maturityDate: ef.MaturityDate, note: null, unpunctuality: true,
            created: ef.Created, updated: DateTime.UtcNow);

        await Sut.UpdateEntityAsync(fixedIncome, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        EfFixedIncome? updated = await Context.FixedIncomes.FirstOrDefaultAsync(fi => fi.Id == ef.Id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(3500m, updated.Amount);
        Assert.Equal("Salary (Raise)", updated.Content);
        Assert.True(updated.Unpunctuality);
    }

    // -- DeleteByIdAsync -----------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes the item row.</summary>
    [Fact]
    public async Task DeleteByIdAsync_RemovesItemFromDatabase()
    {
        string alice = UniqueEmail("alice");
        var ef = MakeFixedIncome(alice, "Pension");
        await SeedAsync(ef);

        await Sut.DeleteByIdAsync(ef.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Null(await Context.FixedIncomes.FirstOrDefaultAsync(fi => fi.Id == ef.Id, TestContext.Current.CancellationToken));
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other items.</summary>
    [Fact]
    public async Task DeleteByIdAsync_DoesNotAffectOtherItems()
    {
        string alice = UniqueEmail("alice");
        var keep = MakeFixedIncome(alice, "Salary");
        var remove = MakeFixedIncome(alice, "Side job");
        await SeedAsync(keep, remove);

        await Sut.DeleteByIdAsync(remove.Id, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        List<EfFixedIncome> remaining = await Context.FixedIncomes
            .Where(fi => fi.AccountEmail == alice).ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(keep.Id, Assert.Single(remaining).Id);
    }
}
