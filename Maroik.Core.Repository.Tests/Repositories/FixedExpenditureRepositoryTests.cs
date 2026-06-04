using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using EfFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;

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

    // -- Helpers --------------------------------------------------------------

    private static EfFixedExpenditure MakeFixedExpenditure(string email, string content, decimal amount = 50m) => new()
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

    private async Task SeedAsync(params EfFixedExpenditure[] items)
    {
        await _context.FixedExpenditures.AddRangeAsync(items);
        await _context.SaveChangesAsync();
    }

    // -- GetFixedExpendituresAsync --------------------------------------------

    /// <summary>Verifies that <c>GetFixedExpendituresAsync</c> returns only items for given email.</summary>
    [Fact]
    public async Task GetFixedExpendituresAsync_ReturnsOnlyItemsForGivenEmail()
    {
        await SeedAsync(
            MakeFixedExpenditure("alice@example.com", "Netflix"),
            MakeFixedExpenditure("alice@example.com", "Insurance"),
            MakeFixedExpenditure("bob@example.com", "Gym"));

        List<FixedExpenditure> result = await _sut.GetByAccountEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, fe => Assert.Equal("alice@example.com", fe.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetFixedExpendituresAsync</c> returns empty when no items for email.</summary>
    [Fact]
    public async Task GetFixedExpendituresAsync_ReturnsEmpty_WhenNoItemsForEmail()
    {
        List<FixedExpenditure> result = await _sut.GetByAccountEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- CreateFixedExpenditureAsync ------------------------------------------

    /// <summary>Verifies that <c>CreateFixedExpenditureAsync</c> adds item to database.</summary>
    [Fact]
    public async Task CreateFixedExpenditureAsync_AddsItemToDatabase()
    {
        var fixedExpenditure = FixedExpenditure.Reconstitute(
            id: 0,
            accountEmail: "alice@example.com",
            mainClass: "ConsumerSpending",
            subClass: "Tax",
            content: "Property tax",
            amount: 200m,
            monetaryUnit: string.Empty,
            paymentMethod: "BankAccount",
            myDepositAsset: "BankAccount",
            depositMonth: 3,
            depositDay: 31,
            maturityDate: DateTime.UtcNow.AddYears(1),
            note: null,
            unpunctuality: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.CreateAsync(fixedExpenditure, TestContext.Current.CancellationToken);

        EfFixedExpenditure? saved = await _context.FixedExpenditures.FirstOrDefaultAsync(
            fe => fe.AccountEmail == "alice@example.com" && fe.Content == "Property tax", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(200m, saved.Amount);
        Assert.Equal((short)3, saved.DepositMonth);
        Assert.Equal((short)31, saved.DepositDay);
    }

    // -- UpdateEntityAsync ------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing item.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingItem()
    {
        var efItem = MakeFixedExpenditure("alice@example.com", "Netflix", amount: 50m);
        await SeedAsync(efItem);
        long id = efItem.Id;

        var fixedExpenditure = FixedExpenditure.Reconstitute(
            id: id,
            accountEmail: "alice@example.com",
            mainClass: "ConsumerSpending",
            subClass: "Tax",
            content: "Netflix Premium",
            amount: 75m,
            monetaryUnit: string.Empty,
            paymentMethod: "Wallet",
            myDepositAsset: "Wallet",
            depositMonth: 0,
            depositDay: 25,
            maturityDate: efItem.MaturityDate,
            note: null,
            unpunctuality: true,
            created: efItem.Created,
            updated: DateTime.UtcNow);

        await _sut.UpdateEntityAsync(fixedExpenditure, TestContext.Current.CancellationToken);

        EfFixedExpenditure? updated = await _context.FixedExpenditures.FirstOrDefaultAsync(fe => fe.Id == id, TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(75m, updated.Amount);
        Assert.Equal("Netflix Premium", updated.Content);
        Assert.True(updated.Unpunctuality);
    }

    // -- DeleteByIdAsync ------------------------------------------

    /// <summary>Verifies that <c>DeleteByIdAsync</c> removes item from database.</summary>
    [Fact]
    public async Task DeleteFixedExpenditureAsync_RemovesItemFromDatabase()
    {
        var efItem = MakeFixedExpenditure("alice@example.com", "Spotify");
        await SeedAsync(efItem);
        long id = efItem.Id;

        await _sut.DeleteByIdAsync(id, TestContext.Current.CancellationToken);

        EfFixedExpenditure? deleted = await _context.FixedExpenditures.FirstOrDefaultAsync(fe => fe.Id == id, TestContext.Current.CancellationToken);
        Assert.Null(deleted);
    }

    /// <summary>Verifies that <c>DeleteByIdAsync</c> does not affect other items.</summary>
    [Fact]
    public async Task DeleteFixedExpenditureAsync_DoesNotAffectOtherItems()
    {
        var efItem1 = MakeFixedExpenditure("alice@example.com", "Netflix");
        var efItem2 = MakeFixedExpenditure("alice@example.com", "Spotify");
        await SeedAsync(efItem1, efItem2);

        await _sut.DeleteByIdAsync(efItem1.Id, TestContext.Current.CancellationToken);

        List<EfFixedExpenditure> remaining = await _context.FixedExpenditures.ToListAsync(TestContext.Current.CancellationToken);
        Assert.Single(remaining);
        Assert.Equal(efItem2.Id, remaining[0].Id);
    }

    // UpdateMyDepositAssetNameAsync — tested in FixedExpenditureRepositoryIntegrationTests
    // because HasDefaultValueSql on MyDepositAsset causes EF Core InMemory to omit the column
    // on INSERT, making the Where clause always return zero rows.
}
