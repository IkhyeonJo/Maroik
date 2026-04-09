using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="FixedIncomeRepository"/> using an EF Core InMemory database.
/// </summary>
public class FixedIncomeRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FixedIncomeRepository _sut;

    public FixedIncomeRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new FixedIncomeRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static FixedIncome MakeFixedIncome(string email, string content, decimal amount = 3000m) => new()
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

    private async Task SeedAsync(params FixedIncome[] items)
    {
        await _context.FixedIncomes.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    // ── GetFixedIncomesAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetFixedIncomesAsync_ReturnsOnlyItemsForGivenEmail()
    {
        await SeedAsync(
            MakeFixedIncome("alice@example.com", "Salary"),
            MakeFixedIncome("alice@example.com", "Side job"),
            MakeFixedIncome("bob@example.com", "Bob salary"));

        List<FixedIncomeResponse> result = await _sut.GetFixedIncomesAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, fi => Assert.Equal("alice@example.com", fi.AccountEmail));
    }

    [Fact]
    public async Task GetFixedIncomesAsync_ReturnsEmpty_WhenNoItemsForEmail()
    {
        List<FixedIncomeResponse> result = await _sut.GetFixedIncomesAsync("nobody@example.com");

        Assert.Empty(result);
    }

    // ── CreateFixedIncomeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateFixedIncomeAsync_AddsItemToDatabase()
    {
        var request = new FixedIncomeRequest
        {
            AccountEmail = "alice@example.com",
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Monthly salary",
            Amount = 5000m,
            DepositMyAssetProductName = "BankAccount",
            DepositMonth = 0,
            DepositDay = 25,
            MaturityDate = DateTime.UtcNow.AddYears(2),
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Unpunctuality = false
        };

        await _sut.CreateFixedIncomeAsync(request);

        FixedIncome? saved = await _context.FixedIncomes.FirstOrDefaultAsync(
            fi => fi.AccountEmail == "alice@example.com" && fi.Content == "Monthly salary");
        Assert.NotNull(saved);
        Assert.Equal(5000m, saved.Amount);
        Assert.Equal((short)25, saved.DepositDay);
    }

    // ── UpdateFixedIncomeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateFixedIncomeAsync_UpdatesExistingItem()
    {
        var item = MakeFixedIncome("alice@example.com", "Salary", amount: 3000m);
        await SeedAsync(item);
        long id = item.Id;

        var request = new FixedIncomeRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Salary (Raise)",
            Amount = 3500m,
            DepositMyAssetProductName = "BankAccount",
            DepositMonth = 0,
            DepositDay = 25,
            MaturityDate = item.MaturityDate,
            Created = item.Created,
            Updated = DateTime.UtcNow,
            Unpunctuality = true
        };

        await _sut.UpdateFixedIncomeAsync(request);

        FixedIncome? updated = await _context.FixedIncomes.FirstOrDefaultAsync(fi => fi.Id == id);
        Assert.NotNull(updated);
        Assert.Equal(3500m, updated.Amount);
        Assert.Equal("Salary (Raise)", updated.Content);
        Assert.True(updated.Unpunctuality);
    }

    // ── DeleteFixedIncomeAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteFixedIncomeAsync_RemovesItemFromDatabase()
    {
        var item = MakeFixedIncome("alice@example.com", "Pension");
        await SeedAsync(item);
        long id = item.Id;

        var request = new FixedIncomeRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Pension",
            Amount = 3000m,
            DepositMyAssetProductName = "BankAccount",
            DepositMonth = 0,
            DepositDay = 25,
            MaturityDate = item.MaturityDate,
            Created = item.Created,
            Updated = item.Updated
        };

        await _sut.DeleteFixedIncomeAsync(request);

        FixedIncome? deleted = await _context.FixedIncomes.FirstOrDefaultAsync(fi => fi.Id == id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteFixedIncomeAsync_DoesNotAffectOtherItems()
    {
        var item1 = MakeFixedIncome("alice@example.com", "Salary");
        var item2 = MakeFixedIncome("alice@example.com", "Side job");
        await SeedAsync(item1, item2);

        var request = new FixedIncomeRequest
        {
            Id = item1.Id,
            AccountEmail = "alice@example.com",
            MainClass = "RegularIncome",
            SubClass = "LaborIncome",
            Content = "Salary",
            Amount = 3000m,
            DepositMyAssetProductName = "BankAccount",
            DepositMonth = 0,
            DepositDay = 25,
            MaturityDate = item1.MaturityDate,
            Created = item1.Created,
            Updated = item1.Updated
        };

        await _sut.DeleteFixedIncomeAsync(request);

        List<FixedIncome> remaining = await _context.FixedIncomes.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(item2.Id, remaining[0].Id);
    }
}
