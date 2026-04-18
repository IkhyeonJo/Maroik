using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

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

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Income MakeIncome(string email, string assetName, decimal amount = 500m) => new()
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

    private async Task SeedAsync(params Income[] incomes)
    {
        await _context.Incomes.AddRangeAsync(incomes);
        await _context.SaveChangesAsync();
    }

    // ── GetIncomesAsync ──────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetIncomesAsync</c> returns only incomes for given email.</summary>
    [Fact]
    public async Task GetIncomesAsync_ReturnsOnlyIncomesForGivenEmail()
    {
        await SeedAsync(
            MakeIncome("alice@example.com", "SavingsA"),
            MakeIncome("alice@example.com", "SavingsB"),
            MakeIncome("bob@example.com", "BobSavings"));

        List<IncomeResponse> result = await _sut.GetIncomesAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, i => Assert.Equal("alice@example.com", i.AccountEmail));
    }

    /// <summary>Verifies that <c>GetIncomesAsync</c> returns empty when no incomes for email.</summary>
    [Fact]
    public async Task GetIncomesAsync_ReturnsEmpty_WhenNoIncomesForEmail()
    {
        List<IncomeResponse> result = await _sut.GetIncomesAsync("nobody@example.com");

        Assert.Empty(result);
    }

    // ── CreateIncomeAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>CreateIncomeAsync</c> adds income to database.</summary>
    [Fact]
    public async Task CreateIncomeAsync_AddsIncomeToDatabase()
    {
        var request = new IncomeRequest
        {
            AccountEmail = "alice@example.com",
            DepositMyAssetProductName = "SavingsAccount",
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Monthly salary",
            Amount = 3000m,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        await _sut.CreateIncomeAsync(request);

        Income? saved = await _context.Incomes.FirstOrDefaultAsync(
            i => i.AccountEmail == "alice@example.com" && i.Content == "Monthly salary", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(3000m, saved.Amount);
    }

    // ── UpdateIncomeAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateIncomeAsync</c> updates existing income.</summary>
    [Fact]
    public async Task UpdateIncomeAsync_UpdatesExistingIncome()
    {
        var income = MakeIncome("alice@example.com", "SavingsAccount", amount: 500m);
        await SeedAsync(income);
        long id = income.Id;

        var request = new IncomeRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            DepositMyAssetProductName = "SavingsAccount",
            MainClass = "RegularIncome",
            SubClass = "BusinessIncome",
            Content = "Updated Content",
            Amount = 750m,
            Created = income.Created,
            Updated = DateTime.UtcNow
        };

        await _sut.UpdateIncomeAsync(request);

        Income? updated = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(750m, updated.Amount);
        Assert.Equal("BusinessIncome", updated.SubClass);
    }

    // ── DeleteIncomeAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteIncomeAsync</c> removes income from database.</summary>
    [Fact]
    public async Task DeleteIncomeAsync_RemovesIncomeFromDatabase()
    {
        var income = MakeIncome("alice@example.com", "SavingsAccount");
        await SeedAsync(income);
        long id = income.Id;

        var request = new IncomeRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            DepositMyAssetProductName = "SavingsAccount",
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Salary",
            Amount = 500m
        };

        await _sut.DeleteIncomeAsync(request);

        Income? deleted = await _context.Incomes.FirstOrDefaultAsync(i => i.Id == id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteIncomeAsync</c> does not affect other incomes.</summary>
    [Fact]
    public async Task DeleteIncomeAsync_DoesNotAffectOtherIncomes()
    {
        var income1 = MakeIncome("alice@example.com", "SavingsA");
        var income2 = MakeIncome("alice@example.com", "SavingsB");
        await SeedAsync(income1, income2);

        var request = new IncomeRequest
        {
            Id = income1.Id,
            AccountEmail = "alice@example.com",
            DepositMyAssetProductName = "SavingsA",
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Salary",
            Amount = 500m
        };

        await _sut.DeleteIncomeAsync(request);

        List<Income> remaining = await _context.Incomes.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(income2.Id, remaining[0].Id);
    }
}
