using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using EfFixedExpenditure = Maroik.Core.PostgreSQL.Models.FixedExpenditure;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="FixedExpenditureRepository"/> against a real PostgreSQL instance.
/// Focuses on <c>UpdateMyDepositAssetNameAsync</c> — EF Core's <c>HasDefaultValueSql</c> on the
/// <c>MyDepositAsset</c> column causes the InMemory provider to omit the value on INSERT,
/// making the LINQ Where clause always return zero rows in unit tests.
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
        // Npgsql 9+ requires DateTimeKind.Unspecified for timestamp-without-timezone columns.
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

    private static OrmAccount MakeAccount(string email) => new()
    {
        Email = email,
        HashedPassword = "hashed",
        Nickname = email,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private static OrmAsset MakeAsset(string productName, string email) => new()
    {
        ProductName = productName,
        AccountEmail = email,
        Item = "FreeDepositAndWithdrawal",
        Amount = 0m,
        MonetaryUnit = "KRW",
        Deleted = false,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private static EfFixedExpenditure MakeOrm(string email, string content, string depositAsset, decimal amount = 50m) => new()
    {
        AccountEmail = email,
        MainClass = "ConsumerSpending",
        SubClass = "Tax",
        Content = content,
        Amount = amount,
        PaymentMethod = "Wallet",
        MyDepositAsset = depositAsset,
        DepositMonth = 0,
        DepositDay = 25,
        MaturityDate = DateTime.UtcNow.AddYears(1),
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow,
        Unpunctuality = false
    };

    private async Task SeedAsync(params EfFixedExpenditure[] items)
    {
        // FK chain: Account ← Asset(ProductName=PaymentMethod) ← FixedExpenditure
        // Asset_fk_0: Asset.AccountEmail → Account.Email
        // FixedExpenditure_fk_0: (PaymentMethod, AccountEmail) → Asset.(ProductName, AccountEmail)

        var emails = items.Select(i => i.AccountEmail).Distinct().ToList();
        var existingEmails = await _context.Accounts.Select(a => a.Email).ToListAsync();
        var newAccounts = emails.Where(e => !existingEmails.Contains(e)).Select(e => MakeAccount(e!)).ToList();
        if (newAccounts.Count > 0)
        {
            await _context.Accounts.AddRangeAsync(newAccounts);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        var paymentMethods = items.Select(i => i.PaymentMethod).Distinct().ToList();
        var assetsToAdd = new List<OrmAsset>();
        foreach (var email in emails)
        {
            foreach (var pm in paymentMethods)
            {
                bool exists = await _context.Assets.AnyAsync(a => a.AccountEmail == email && a.ProductName == pm);
                if (!exists) assetsToAdd.Add(MakeAsset(pm!, email!));
            }
        }
        if (assetsToAdd.Count > 0)
        {
            await _context.Assets.AddRangeAsync(assetsToAdd);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        await _context.FixedExpenditures.AddRangeAsync(items);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    // -- UpdateMyDepositAssetNameAsync ----------------------------------------

    /// <summary>Verifies that <c>UpdateMyDepositAssetNameAsync</c> renames all matching deposit assets.</summary>
    [Fact]
    public async Task UpdateMyDepositAssetNameAsync_RenamesAllMatchingItems()
    {
        await SeedAsync(
            MakeOrm("alice@example.com", "Netflix", "OldWallet", 12m),
            MakeOrm("alice@example.com", "Spotify", "OldWallet", 10m),
            MakeOrm("alice@example.com", "Gym", "OtherAsset", 30m));

        await _sut.UpdateMyDepositAssetNameAsync("alice@example.com", "OldWallet", "NewWallet", TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        List<EfFixedExpenditure> all = await _context.FixedExpenditures.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.Equal(2, all.Count(fe => fe.MyDepositAsset == "NewWallet"));
        Assert.Equal(1, all.Count(fe => fe.MyDepositAsset == "OtherAsset"));
        Assert.DoesNotContain(all, fe => fe.MyDepositAsset == "OldWallet");
    }

    /// <summary>Verifies that <c>UpdateMyDepositAssetNameAsync</c> does not affect other users' items.</summary>
    [Fact]
    public async Task UpdateMyDepositAssetNameAsync_DoesNotAffect_OtherUsersItems()
    {
        await SeedAsync(
            MakeOrm("alice@example.com", "Netflix", "Wallet"),
            MakeOrm("bob@example.com", "Gym", "Wallet"));

        await _sut.UpdateMyDepositAssetNameAsync("alice@example.com", "Wallet", "AliceNewWallet", TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        EfFixedExpenditure? bobItem = await _context.FixedExpenditures.AsNoTracking()
            .FirstOrDefaultAsync(fe => fe.AccountEmail == "bob@example.com", TestContext.Current.CancellationToken);
        Assert.NotNull(bobItem);
        Assert.Equal("Wallet", bobItem.MyDepositAsset);
    }

    /// <summary>Verifies that <c>UpdateMyDepositAssetNameAsync</c> does nothing when no items match.</summary>
    [Fact]
    public async Task UpdateMyDepositAssetNameAsync_DoesNothing_WhenNoItemsMatch()
    {
        await SeedAsync(MakeOrm("alice@example.com", "Netflix", "ExistingWallet"));

        await _sut.UpdateMyDepositAssetNameAsync("alice@example.com", "NonExistent", "NewWallet", TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        EfFixedExpenditure? found = await _context.FixedExpenditures.AsNoTracking()
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        Assert.NotNull(found);
        Assert.Equal("ExistingWallet", found.MyDepositAsset);
    }
}
