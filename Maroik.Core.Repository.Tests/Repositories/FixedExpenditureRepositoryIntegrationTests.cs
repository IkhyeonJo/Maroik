using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;
using OrmFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="FixedExpenditureRepository"/> against a real PostgreSQL
/// instance (spun up via Testcontainers). Focuses on <c>UpdateMyDepositAssetWithProductNameAsync</c>,
/// which uses <c>ExecuteUpdateAsync</c> — a translation the EF Core InMemory provider does not
/// support. This cascade is what keeps a renamed asset's stored <c>MyDepositAsset</c> references
/// in sync, since that column (unlike <c>PaymentMethod</c>) has no <c>ON UPDATE CASCADE</c> foreign
/// key — it also holds an empty string for transfer-less fixed expenditures, which a FK can't
/// accommodate.
/// </summary>
[Trait("Category", "Integration")]
public class FixedExpenditureRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    private ApplicationDbContext _context = null!;
    private FixedExpenditureRepository _sut = null!;

    /// <summary>Starts the Testcontainers PostgreSQL instance and creates the schema before each test.</summary>
    public async ValueTask InitializeAsync()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        await _postgres.StartAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        _context = new ApplicationDbContext(options);
        await _context.Database.EnsureCreatedAsync();
        _sut = new FixedExpenditureRepository(_context);
    }

    /// <summary>Disposes the database context and stops the PostgreSQL container after each test.</summary>
    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    // -- Helpers --------------------------------------------------------------

    private static OrmFixedExpenditure MakeOrm(string email, string paymentMethod, string myDepositAsset, string content = "Test") => new()
    {
        AccountEmail = email,
        MainClass = "ConsumerSpending",
        SubClass = "Tax",
        Content = content,
        Amount = 50m,
        PaymentMethod = paymentMethod,
        MyDepositAsset = myDepositAsset,
        DepositMonth = 0,
        DepositDay = 25,
        MaturityDate = DateTime.UtcNow.AddYears(1),
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        Unpunctuality = false
    };

    /// <summary>
    /// Seeds the Account and Asset rows a given <see cref="OrmFixedExpenditure.PaymentMethod"/>
    /// requires (FixedExpenditure_fk_0), then the fixed expenditure rows themselves.
    /// </summary>
    private async Task SeedAsync(params OrmFixedExpenditure[] items)
    {
        var emails = items.Select(e => e.AccountEmail).Distinct().ToList();
        var existingEmails = await _context.Accounts.Select(a => a.Email).ToListAsync();
        var newAccounts = emails.Where(e => !existingEmails.Contains(e))
            .Select(e => new OrmAccount { Email = e!, HashedPassword = "hashed", Nickname = e, Created = DateTime.UtcNow, Updated = DateTime.UtcNow })
            .ToList();
        if (newAccounts.Count > 0)
        {
            await _context.Accounts.AddRangeAsync(newAccounts);
            await _context.SaveChangesAsync();
        }

        var paymentMethods = items.Select(e => (e.PaymentMethod!, e.AccountEmail!)).Distinct().ToList();
        var existingAssets = await _context.Assets.Select(a => new { a.ProductName, a.AccountEmail }).ToListAsync();
        var newAssets = paymentMethods
            .Where(pm => !existingAssets.Any(a => a.ProductName == pm.Item1 && a.AccountEmail == pm.Item2))
            .Select(pm => new OrmAsset
            {
                ProductName = pm.Item1,
                AccountEmail = pm.Item2,
                Item = "FreeDepositAndWithdrawal",
                MonetaryUnit = "KRW",
                Amount = 0,
                Created = DateTime.UtcNow,
                Updated = DateTime.UtcNow
            })
            .ToList();
        if (newAssets.Count > 0)
        {
            await _context.Assets.AddRangeAsync(newAssets);
            await _context.SaveChangesAsync();
        }

        await _context.FixedExpenditures.AddRangeAsync(items);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    // -- UpdateMyDepositAssetWithProductNameAsync ------------------------------

    /// <summary>
    /// Verifies that renaming an asset cascades to every fixed expenditure referencing it as
    /// <c>MyDepositAsset</c> for that account, and returns the number of rows updated.
    /// </summary>
    [Fact]
    public async Task UpdateMyDepositAssetWithProductNameAsync_RenamesMatchingRows()
    {
        var fe1 = MakeOrm("alice@example.com", "Wallet", "OldName", "Netflix");
        var fe2 = MakeOrm("alice@example.com", "Wallet", "OldName", "Spotify");
        var fe3 = MakeOrm("alice@example.com", "Wallet", "OtherAsset", "Unrelated");
        await SeedAsync(fe1, fe2, fe3);

        int updated = await _sut.UpdateMyDepositAssetWithProductNameAsync("alice@example.com", "OldName", "NewName", TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        Assert.Equal(2, updated);
        Assert.Equal("NewName", (await _context.FixedExpenditures.FirstAsync(e => e.Id == fe1.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
        Assert.Equal("NewName", (await _context.FixedExpenditures.FirstAsync(e => e.Id == fe2.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
        Assert.Equal("OtherAsset", (await _context.FixedExpenditures.FirstAsync(e => e.Id == fe3.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
    }

    /// <summary>
    /// Verifies that the rename is scoped to the given account: another account's fixed
    /// expenditure referencing an asset of the same name is left untouched.
    /// </summary>
    [Fact]
    public async Task UpdateMyDepositAssetWithProductNameAsync_DoesNotAffectOtherAccounts()
    {
        var mine = MakeOrm("alice@example.com", "AliceWallet", "SharedName", "Mine");
        var someoneElses = MakeOrm("bob@example.com", "BobWallet", "SharedName", "Not mine");
        await SeedAsync(mine, someoneElses);

        int updated = await _sut.UpdateMyDepositAssetWithProductNameAsync("alice@example.com", "SharedName", "Renamed", TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        Assert.Equal(1, updated);
        Assert.Equal("Renamed", (await _context.FixedExpenditures.FirstAsync(e => e.Id == mine.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
        Assert.Equal("SharedName", (await _context.FixedExpenditures.FirstAsync(e => e.Id == someoneElses.Id, TestContext.Current.CancellationToken)).MyDepositAsset);
    }

    /// <summary>Verifies that renaming a name with no matching rows affects zero rows.</summary>
    [Fact]
    public async Task UpdateMyDepositAssetWithProductNameAsync_ReturnsZero_WhenNoMatch()
    {
        await SeedAsync(MakeOrm("alice@example.com", "Wallet", "SomeAsset"));

        int updated = await _sut.UpdateMyDepositAssetWithProductNameAsync("alice@example.com", "DoesNotExist", "NewName", TestContext.Current.CancellationToken);

        Assert.Equal(0, updated);
    }
}
