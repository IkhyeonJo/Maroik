using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfIncome = Maroik.Core.PostgreSQL.Models.Income;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="IncomeRepository"/> using an EF Core InMemory database.
/// Raw SQL methods (GetCurrentYearIncomesAsync, GetCurrentYearMonthIncomesAsync) are not tested
/// here because they use PostgreSQL-specific SQL syntax incompatible with the InMemory provider.
/// </summary>
public class IncomeRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly IncomeRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public IncomeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new IncomeRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfIncome MakeIncome(string email, string assetName, decimal amount = 500m) => new()
    {
        AccountEmail = email,
        DepositMyAssetProductName = assetName,
        MainClass = "RegularIncome",
        SubClass = "LaborIncome",
        Content = "Salary",
        Amount = amount,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params EfIncome[] incomes)
    {
        await _context.Incomes.AddRangeAsync(incomes);
        await _context.SaveChangesAsync();
    }

    // -- GetIncomesAsync ------------------------------------------------------

    /// <summary>Verifies that <c>GetIncomesAsync</c> returns only incomes for given email.</summary>
    [Fact]
    public async Task GetIncomesAsync_ReturnsOnlyIncomesForGivenEmail()
    {
        await SeedAsync(
            MakeIncome("alice@example.com", "SavingsA"),
            MakeIncome("alice@example.com", "SavingsB"),
            MakeIncome("bob@example.com", "BobSavings"));

        List<Income> result = await _sut.GetByAccountEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal("alice@example.com", i.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetIncomesAsync</c> returns empty when no incomes for email.</summary>
    [Fact]
    public async Task GetIncomesAsync_ReturnsEmpty_WhenNoIncomesForEmail()
    {
        List<Income> result = await _sut.GetByAccountEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateIncomeAsync ----------------------------------------------------

    /// <summary>Verifies that <c>CreateIncomeAsync</c> adds income to database.</summary>
    [Fact]
    public async Task CreateIncomeAsync_AddsIncomeToDatabase()
    {
        var income = Income.Reconstitute(
            id: 0,
            accountEmail: "alice@example.com",
            mainClass: "RegularIncome",
            subClass: "LaborIncome",
            content: "Monthly salary",
            amount: 3000m,
            monetaryUnit: string.Empty,
            depositMyAssetProductName: "SavingsAccount",
            note: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.CreateAsync(income, TestContext.Current.CancellationToken);

        EfIncome? saved = await _context.Incomes.FirstOrDefaultAsync(
            i => i.AccountEmail == "alice@example.com" && i.Content == "Monthly salary", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(3000m, saved.Amount);
    }

    // -- UpdateEntityAsync ----------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing income.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingIncome()
    {
        var efIncome = MakeIncome("alice@example.com", "SavingsAccount", amount: 500m);
        await SeedAsync(efIncome);
        long id = efIncome.Id;

        var income = Income.Reconstitute(
            id: id,
            accountEmail: "alice@example.com",
            mainClass: "RegularIncome",
            subClass: "BusinessIncome",
            content: "Updated Content",
            amount: 750m,
            monetaryUnit: string.Empty,
            depositMyAssetProductName: "SavingsAccount",
            note: null,
            created: efIncome.Created,
            updated: DateTime.UtcNow);

        await _sut.UpdateEntityAsync(income, TestContext.Current.CancellationToken);

        EfIncome? updated = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(750m, updated.Amount);
        Assert.Equal("BusinessIncome", updated.SubClass);
    }

    // -- DeleteByIdAsync ----------------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes income from database.</summary>
    [Fact]
    public async Task DeleteIncomeAsync_RemovesIncomeFromDatabase()
    {
        var efIncome = MakeIncome("alice@example.com", "SavingsAccount");
        await SeedAsync(efIncome);
        long id = efIncome.Id;

        await _sut.DeleteByIdAsync(id, TestContext.Current.CancellationToken);

        EfIncome? deleted = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other incomes.</summary>
    [Fact]
    public async Task DeleteIncomeAsync_DoesNotAffectOtherIncomes()
    {
        var efIncome1 = MakeIncome("alice@example.com", "SavingsA");
        var efIncome2 = MakeIncome("alice@example.com", "SavingsB");
        await SeedAsync(efIncome1, efIncome2);

        await _sut.DeleteByIdAsync(efIncome1.Id, TestContext.Current.CancellationToken);

        List<EfIncome> remaining = await _context.Incomes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(efIncome2.Id, remaining[0].Id);
    }
}
