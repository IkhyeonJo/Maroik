using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfAsset = Maroik.Core.PostgreSQL.Models.Asset;
using EfExpenditure = Maroik.Core.PostgreSQL.Models.Expenditure;

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

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public ExpenditureRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new ExpenditureRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static EfExpenditure MakeExpenditure(string email, string content, decimal amount = 100m) => new()
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

    private async Task SeedAsync(params EfExpenditure[] expenditures)
    {
        await _context.Expenditures.AddRangeAsync(expenditures);
        await _context.SaveChangesAsync();
    }

    // -- GetExpendituresAsync -------------------------------------------------

    /// <summary>Verifies that <c>GetExpendituresAsync</c> returns only expenditures for given email.</summary>
    [Fact]
    public async Task GetExpendituresAsync_ReturnsOnlyExpendituresForGivenEmail()
    {
        // Include(x => x.Asset) requires matching Asset rows to exist in InMemory; seed them first.
        await _context.Assets.AddRangeAsync(
            new EfAsset { ProductName = "Wallet", AccountEmail = "alice@example.com", Item = "Wallet", MonetaryUnit = "KRW", Amount = 0, Created = DateTime.UtcNow, Updated = DateTime.UtcNow },
            new EfAsset { ProductName = "Wallet", AccountEmail = "bob@example.com", Item = "Wallet", MonetaryUnit = "KRW", Amount = 0, Created = DateTime.UtcNow, Updated = DateTime.UtcNow });
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await SeedAsync(
            MakeExpenditure("alice@example.com", "Lunch"),
            MakeExpenditure("alice@example.com", "Dinner"),
            MakeExpenditure("bob@example.com", "Coffee"));

        List<Expenditure> result = await _sut.GetByAccountEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal("alice@example.com", e.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetExpendituresAsync</c> returns empty when no expenditures for email.</summary>
    [Fact]
    public async Task GetExpendituresAsync_ReturnsEmpty_WhenNoExpendituresForEmail()
    {
        List<Expenditure> result = await _sut.GetByAccountEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateExpenditureAsync -----------------------------------------------

    /// <summary>Verifies that <c>CreateExpenditureAsync</c> adds expenditure to database.</summary>
    [Fact]
    public async Task CreateExpenditureAsync_AddsExpenditureToDatabase()
    {
        var expenditure = Expenditure.Reconstitute(
            id: 0,
            accountEmail: "alice@example.com",
            mainClass: "ConsumerSpending",
            subClass: "MealOrEatOutExpenses",
            content: "Team lunch",
            amount: 250m,
            monetaryUnit: string.Empty,
            paymentMethod: "CreditCard",
            myDepositAsset: "CreditCard",
            note: null,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.CreateAsync(expenditure, TestContext.Current.CancellationToken);

        EfExpenditure? saved = await _context.Expenditures.FirstOrDefaultAsync(
            e => e.AccountEmail == "alice@example.com" && e.Content == "Team lunch", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(250m, saved.Amount);
    }

    // -- UpdateEntityAsync -----------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing expenditure.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingExpenditure()
    {
        var efExpenditure = MakeExpenditure("alice@example.com", "Lunch", amount: 100m);
        await SeedAsync(efExpenditure);
        long id = efExpenditure.Id;

        var expenditure = Expenditure.Reconstitute(
            id: id,
            accountEmail: "alice@example.com",
            mainClass: "ConsumerSpending",
            subClass: "MealOrEatOutExpenses",
            content: "Updated Lunch",
            amount: 150m,
            monetaryUnit: string.Empty,
            paymentMethod: "Wallet",
            myDepositAsset: "Wallet",
            note: null,
            created: efExpenditure.Created,
            updated: DateTime.UtcNow);

        await _sut.UpdateEntityAsync(expenditure, TestContext.Current.CancellationToken);

        EfExpenditure? updated = await _context.Expenditures.FirstOrDefaultAsync(e => e.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(150m, updated.Amount);
        Assert.Equal("Updated Lunch", updated.Content);
    }

    // -- DeleteByIdAsync -----------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes expenditure from database.</summary>
    [Fact]
    public async Task DeleteExpenditureAsync_RemovesExpenditureFromDatabase()
    {
        var efExpenditure = MakeExpenditure("alice@example.com", "Taxi");
        await SeedAsync(efExpenditure);
        long id = efExpenditure.Id;

        await _sut.DeleteByIdAsync(id, TestContext.Current.CancellationToken);

        EfExpenditure? deleted = await _context.Expenditures.FirstOrDefaultAsync(e => e.Id == id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other expenditures.</summary>
    [Fact]
    public async Task DeleteExpenditureAsync_DoesNotAffectOtherExpenditures()
    {
        var efExp1 = MakeExpenditure("alice@example.com", "Lunch");
        var efExp2 = MakeExpenditure("alice@example.com", "Dinner");
        await SeedAsync(efExp1, efExp2);

        await _sut.DeleteByIdAsync(efExp1.Id, TestContext.Current.CancellationToken);

        List<EfExpenditure> remaining = await _context.Expenditures.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(efExp2.Id, remaining[0].Id);
    }
}
