using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfFixedIncome = Maroik.Core.PostgreSQL.Models.FixedIncome;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="FixedIncomeRepository"/> using an EF Core InMemory database.
/// </summary>
public class FixedIncomeRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FixedIncomeRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public FixedIncomeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new FixedIncomeRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfFixedIncome MakeFixedIncome(string email, string content, decimal amount = 3000m) => new()
    {
        AccountEmail = email,
        MainClass = "RegularIncome",
        SubClass = "LaborIncome",
        Content = content,
        Amount = amount,
        DepositMyAssetProductName = "BankAccount",
        DepositMonth = 0,
        DepositDay = 25,
        MaturityDate = DateTime.UtcNow.AddYears(1),
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        Unpunctuality = false
    };

    private async Task SeedAsync(params EfFixedIncome[] items)
    {
        await _context.FixedIncomes.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    // -- GetFixedIncomesAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetFixedIncomesAsync</c> returns only items for given email.</summary>
    [Fact]
    public async Task GetFixedIncomesAsync_ReturnsOnlyItemsForGivenEmail()
    {
        // FixedIncomeRepository eager-loads Asset (required relationship), so a matching Asset row
        // must exist for the InMemory provider to return the FixedIncome rows via Include.
        await _context.Assets.AddRangeAsync(
            new Maroik.Core.PostgreSQL.Models.Asset { ProductName = "BankAccount", AccountEmail = "alice@example.com", Item = "Bank", MonetaryUnit = "KRW", Amount = 0, Created = DateTime.UtcNow, Updated = DateTime.UtcNow },
            new Maroik.Core.PostgreSQL.Models.Asset { ProductName = "BankAccount", AccountEmail = "bob@example.com", Item = "Bank", MonetaryUnit = "KRW", Amount = 0, Created = DateTime.UtcNow, Updated = DateTime.UtcNow });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SeedAsync(
            MakeFixedIncome("alice@example.com", "Salary"),
            MakeFixedIncome("alice@example.com", "Side job"),
            MakeFixedIncome("bob@example.com", "Bob salary"));

        List<FixedIncome> result = await _sut.GetByAccountEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, fi => Assert.Equal("alice@example.com", fi.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetFixedIncomesAsync</c> returns empty when no items for email.</summary>
    [Fact]
    public async Task GetFixedIncomesAsync_ReturnsEmpty_WhenNoItemsForEmail()
    {
        List<FixedIncome> result = await _sut.GetByAccountEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateFixedIncomeAsync -----------------------------------------------

    /// <summary>Verifies that <c>CreateFixedIncomeAsync</c> adds item to database.</summary>
    [Fact]
    public async Task CreateFixedIncomeAsync_AddsItemToDatabase()
    {
        var fixedIncome = FixedIncome.Reconstitute(
            id: 0,
            accountEmail: "alice@example.com",
            mainClass: "RegularIncome",
            subClass: "LaborIncome",
            content: "Monthly salary",
            amount: 5000m,
            monetaryUnit: "",
            depositMyAssetProductName: "BankAccount",
            depositMonth: 0,
            depositDay: 25,
            maturityDate: DateTime.UtcNow.AddYears(2),
            note: null,
            unpunctuality: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.CreateAsync(fixedIncome, TestContext.Current.CancellationToken);

        EfFixedIncome? saved = await _context.FixedIncomes.FirstOrDefaultAsync(
            fi => fi.AccountEmail == "alice@example.com" && fi.Content == "Monthly salary", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(5000m, saved.Amount);
        Assert.Equal((short)25, saved.DepositDay);
    }

    // -- UpdateEntityAsync -----------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing item.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingItem()
    {
        var efItem = MakeFixedIncome("alice@example.com", "Salary", amount: 3000m);
        await SeedAsync(efItem);
        long id = efItem.Id;

        var fixedIncome = FixedIncome.Reconstitute(
            id: id,
            accountEmail: "alice@example.com",
            mainClass: "RegularIncome",
            subClass: "LaborIncome",
            content: "Salary (Raise)",
            amount: 3500m,
            monetaryUnit: "",
            depositMyAssetProductName: "BankAccount",
            depositMonth: 0,
            depositDay: 25,
            maturityDate: efItem.MaturityDate,
            note: null,
            unpunctuality: true,
            created: efItem.Created,
            updated: DateTime.UtcNow);

        await _sut.UpdateEntityAsync(fixedIncome, TestContext.Current.CancellationToken);

        EfFixedIncome? updated = await _context.FixedIncomes.FirstOrDefaultAsync(fi => fi.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(3500m, updated.Amount);
        Assert.Equal("Salary (Raise)", updated.Content);
        Assert.True(updated.Unpunctuality);
    }

    // -- DeleteByIdAsync -----------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes item from database.</summary>
    [Fact]
    public async Task DeleteFixedIncomeAsync_RemovesItemFromDatabase()
    {
        var efItem = MakeFixedIncome("alice@example.com", "Pension");
        await SeedAsync(efItem);
        long id = efItem.Id;

        await _sut.DeleteByIdAsync(id, TestContext.Current.CancellationToken);

        EfFixedIncome? deleted = await _context.FixedIncomes.FirstOrDefaultAsync(fi => fi.Id == id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other items.</summary>
    [Fact]
    public async Task DeleteFixedIncomeAsync_DoesNotAffectOtherItems()
    {
        var efItem1 = MakeFixedIncome("alice@example.com", "Salary");
        var efItem2 = MakeFixedIncome("alice@example.com", "Side job");
        await SeedAsync(efItem1, efItem2);

        await _sut.DeleteByIdAsync(efItem1.Id, TestContext.Current.CancellationToken);

        List<EfFixedIncome> remaining = await _context.FixedIncomes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(efItem2.Id, remaining[0].Id);
    }
}
