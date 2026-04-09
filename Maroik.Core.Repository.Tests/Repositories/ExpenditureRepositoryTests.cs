using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="ExpenditureRepository"/> using an EF Core InMemory database.
/// Raw SQL methods (GetCurrentYearExpendituresAsync, GetCurrentYearMonthExpendituresAsync,
/// UpdateMyDepositAssetWithProductNameAsync) are not tested here because they use
/// PostgreSQL-specific SQL syntax incompatible with the InMemory provider.
/// </summary>
public class ExpenditureRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ExpenditureRepository _sut;

    public ExpenditureRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new ExpenditureRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Expenditure MakeExpenditure(string email, string content, decimal amount = 100m) => new()
    {
        AccountEmail = email,
        MainClass = "ConsumerSpending",
        SubClass = "MealOrEatOutExpenses",
        Content = content,
        Amount = amount,
        PaymentMethod = "Wallet",
        MyDepositAsset = "Wallet",
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params Expenditure[] expenditures)
    {
        await _context.Expenditures.AddRangeAsync(expenditures);
        await _context.SaveChangesAsync();
    }

    // ── GetExpendituresAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task GetExpendituresAsync_ReturnsOnlyExpendituresForGivenEmail()
    {
        await SeedAsync(
            MakeExpenditure("alice@example.com", "Lunch"),
            MakeExpenditure("alice@example.com", "Dinner"),
            MakeExpenditure("bob@example.com", "Coffee"));

        List<ExpenditureResponse> result = await _sut.GetExpendituresAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("alice@example.com", e.AccountEmail));
    }

    [Fact]
    public async Task GetExpendituresAsync_ReturnsEmpty_WhenNoExpendituresForEmail()
    {
        List<ExpenditureResponse> result = await _sut.GetExpendituresAsync("nobody@example.com");

        Assert.Empty(result);
    }

    // ── CreateExpenditureAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CreateExpenditureAsync_AddsExpenditureToDatabase()
    {
        var request = new ExpenditureRequest
        {
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "MealOrEatOutExpenses",
            Content = "Team lunch",
            Amount = 250m,
            PaymentMethod = "CreditCard",
            MyDepositAsset = "CreditCard",
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        await _sut.CreateExpenditureAsync(request);

        Expenditure? saved = await _context.Expenditures.FirstOrDefaultAsync(
            e => e.AccountEmail == "alice@example.com" && e.Content == "Team lunch");
        Assert.NotNull(saved);
        Assert.Equal(250m, saved.Amount);
    }

    // ── UpdateExpenditureAsync ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateExpenditureAsync_UpdatesExistingExpenditure()
    {
        var expenditure = MakeExpenditure("alice@example.com", "Lunch", amount: 100m);
        await SeedAsync(expenditure);
        long id = expenditure.Id;

        var request = new ExpenditureRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "MealOrEatOutExpenses",
            Content = "Updated Lunch",
            Amount = 150m,
            PaymentMethod = "Wallet",
            MyDepositAsset = "Wallet",
            Created = expenditure.Created,
            Updated = DateTime.UtcNow
        };

        await _sut.UpdateExpenditureAsync(request);

        Expenditure? updated = await _context.Expenditures.FirstOrDefaultAsync(e => e.Id == id);
        Assert.NotNull(updated);
        Assert.Equal(150m, updated.Amount);
        Assert.Equal("Updated Lunch", updated.Content);
    }

    // ── DeleteExpenditureAsync ───────────────────────────────────────────────

    [Fact]
    public async Task DeleteExpenditureAsync_RemovesExpenditureFromDatabase()
    {
        var expenditure = MakeExpenditure("alice@example.com", "Taxi");
        await SeedAsync(expenditure);
        long id = expenditure.Id;

        var request = new ExpenditureRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "MealOrEatOutExpenses",
            Content = "Taxi",
            Amount = 100m,
            Created = expenditure.Created,
            Updated = expenditure.Updated
        };

        await _sut.DeleteExpenditureAsync(request);

        Expenditure? deleted = await _context.Expenditures.FirstOrDefaultAsync(e => e.Id == id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task DeleteExpenditureAsync_DoesNotAffectOtherExpenditures()
    {
        var exp1 = MakeExpenditure("alice@example.com", "Lunch");
        var exp2 = MakeExpenditure("alice@example.com", "Dinner");
        await SeedAsync(exp1, exp2);

        var request = new ExpenditureRequest
        {
            Id = exp1.Id,
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "MealOrEatOutExpenses",
            Content = "Lunch",
            Amount = 100m,
            Created = exp1.Created,
            Updated = exp1.Updated
        };

        await _sut.DeleteExpenditureAsync(request);

        List<Expenditure> remaining = await _context.Expenditures.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(exp2.Id, remaining[0].Id);
    }
}
