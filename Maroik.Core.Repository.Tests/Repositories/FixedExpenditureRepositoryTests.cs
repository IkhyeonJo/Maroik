using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="FixedExpenditureRepository"/> using an EF Core InMemory database.
/// UpdateMyDepositAssetWithProductNameAsync is not tested here because it uses raw SQL
/// incompatible with the InMemory provider.
/// </summary>
public class FixedExpenditureRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly FixedExpenditureRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public FixedExpenditureRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new FixedExpenditureRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static FixedExpenditure MakeFixedExpenditure(string email, string content, decimal amount = 50m) => new()
    {
        AccountEmail = email,
        MainClass = "ConsumerSpending",
        SubClass = "Tax",
        Content = content,
        Amount = amount,
        PaymentMethod = "Wallet",
        MyDepositAsset = "Wallet",
        DepositMonth = 0,
        DepositDay = 25,
        MaturityDate = DateTime.UtcNow.AddYears(1),
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        Unpunctuality = false
    };

    private async Task SeedAsync(params FixedExpenditure[] items)
    {
        await _context.FixedExpenditures.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    // ── GetFixedExpendituresAsync ────────────────────────────────────────────

    /// <summary>Verifies that <c>GetFixedExpendituresAsync</c> returns only items for given email.</summary>
    [Fact]
    public async Task GetFixedExpendituresAsync_ReturnsOnlyItemsForGivenEmail()
    {
        await SeedAsync(
            MakeFixedExpenditure("alice@example.com", "Netflix"),
            MakeFixedExpenditure("alice@example.com", "Insurance"),
            MakeFixedExpenditure("bob@example.com", "Gym"));

        List<FixedExpenditureResponse> result = await _sut.GetFixedExpendituresAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, fe => Assert.Equal("alice@example.com", fe.AccountEmail));
    }

    /// <summary>Verifies that <c>GetFixedExpendituresAsync</c> returns empty when no items for email.</summary>
    [Fact]
    public async Task GetFixedExpendituresAsync_ReturnsEmpty_WhenNoItemsForEmail()
    {
        List<FixedExpenditureResponse> result = await _sut.GetFixedExpendituresAsync("nobody@example.com");

        Assert.Empty(result);
    }

    // ── CreateFixedExpenditureAsync ──────────────────────────────────────────

    /// <summary>Verifies that <c>CreateFixedExpenditureAsync</c> adds item to database.</summary>
    [Fact]
    public async Task CreateFixedExpenditureAsync_AddsItemToDatabase()
    {
        var request = new FixedExpenditureRequest
        {
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "Tax",
            Content = "Property tax",
            Amount = 200m,
            PaymentMethod = "BankAccount",
            MyDepositAsset = "BankAccount",
            DepositMonth = 3,
            DepositDay = 31,
            MaturityDate = DateTime.UtcNow.AddYears(1),
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Unpunctuality = false
        };

        await _sut.CreateFixedExpenditureAsync(request);

        FixedExpenditure? saved = await _context.FixedExpenditures.FirstOrDefaultAsync(
            fe => fe.AccountEmail == "alice@example.com" && fe.Content == "Property tax", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(200m, saved.Amount);
        Assert.Equal((short)3, saved.DepositMonth);
        Assert.Equal((short)31, saved.DepositDay);
    }

    // ── UpdateFixedExpenditureAsync ──────────────────────────────────────────

    /// <summary>Verifies that <c>UpdateFixedExpenditureAsync</c> updates existing item.</summary>
    [Fact]
    public async Task UpdateFixedExpenditureAsync_UpdatesExistingItem()
    {
        var item = MakeFixedExpenditure("alice@example.com", "Netflix", amount: 50m);
        await SeedAsync(item);
        long id = item.Id;

        var request = new FixedExpenditureRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "Tax",
            Content = "Netflix Premium",
            Amount = 75m,
            PaymentMethod = "Wallet",
            MyDepositAsset = "Wallet",
            DepositMonth = 0,
            DepositDay = 25,
            MaturityDate = item.MaturityDate,
            Created = item.Created,
            Updated = DateTime.UtcNow,
            Unpunctuality = true
        };

        await _sut.UpdateFixedExpenditureAsync(request);

        FixedExpenditure? updated = await _context.FixedExpenditures.FirstOrDefaultAsync(fe => fe.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(75m, updated.Amount);
        Assert.Equal("Netflix Premium", updated.Content);
        Assert.True(updated.Unpunctuality);
    }

    // ── DeleteFixedExpenditureAsync ──────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteFixedExpenditureAsync</c> removes item from database.</summary>
    [Fact]
    public async Task DeleteFixedExpenditureAsync_RemovesItemFromDatabase()
    {
        var item = MakeFixedExpenditure("alice@example.com", "Spotify");
        await SeedAsync(item);
        long id = item.Id;

        var request = new FixedExpenditureRequest
        {
            Id = id,
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "Tax",
            Content = "Spotify",
            Amount = 50m,
            DepositMonth = 0,
            DepositDay = 25,
            MaturityDate = item.MaturityDate,
            Created = item.Created,
            Updated = item.Updated
        };

        await _sut.DeleteFixedExpenditureAsync(request);

        FixedExpenditure? deleted = await _context.FixedExpenditures.FirstOrDefaultAsync(fe => fe.Id == id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteFixedExpenditureAsync</c> does not affect other items.</summary>
    [Fact]
    public async Task DeleteFixedExpenditureAsync_DoesNotAffectOtherItems()
    {
        var item1 = MakeFixedExpenditure("alice@example.com", "Netflix");
        var item2 = MakeFixedExpenditure("alice@example.com", "Spotify");
        await SeedAsync(item1, item2);

        var request = new FixedExpenditureRequest
        {
            Id = item1.Id,
            AccountEmail = "alice@example.com",
            MainClass = "ConsumerSpending",
            SubClass = "Tax",
            Content = "Netflix",
            Amount = 50m,
            DepositMonth = 0,
            DepositDay = 25,
            MaturityDate = item1.MaturityDate,
            Created = item1.Created,
            Updated = item1.Updated
        };

        await _sut.DeleteFixedExpenditureAsync(request);

        List<FixedExpenditure> remaining = await _context.FixedExpenditures.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(item2.Id, remaining[0].Id);
    }
}
