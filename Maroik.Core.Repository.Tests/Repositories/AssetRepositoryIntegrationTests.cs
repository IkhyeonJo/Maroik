using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Integration tests for <see cref="AssetRepository"/> against a real PostgreSQL instance
/// (spun up via Testcontainers). Focuses on <c>UpdateAssetWithProductNameAsync</c>, which
/// uses raw SQL to update a composite primary key (ProductName + AccountEmail) — a scenario
/// that cannot be exercised with the EF Core InMemory provider.
/// </summary>
[Trait("Category", "Integration")]
public class AssetRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:17")
        .Build();

    private ApplicationDbContext _context = null!;
    private AssetRepository _sut = null!;

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
        _sut = new AssetRepository(_context);
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

    private static OrmAsset MakeOrm(string productName, string email, string item = "FreeDepositAndWithdrawal",
        decimal amount = 1000m, string unit = "KRW") => new()
        {
            ProductName = productName,
            AccountEmail = email,
            Item = item,
            Amount = amount,
            MonetaryUnit = unit,
            Deleted = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

    private async Task SeedAsync(params OrmAsset[] assets)
    {
        // Asset_fk_0: Asset.AccountEmail → Account.Email — seed accounts first
        var emails = assets.Select(a => a.AccountEmail).Distinct().ToList();
        var existingEmails = await _context.Accounts.Select(a => a.Email).ToListAsync();
        var newAccounts = emails.Where(e => !existingEmails.Contains(e)).Select(e => MakeAccount(e!)).ToList();
        if (newAccounts.Count > 0)
        {
            await _context.Accounts.AddRangeAsync(newAccounts);
            await _context.SaveChangesAsync();
            _context.ChangeTracker.Clear();
        }

        await _context.Assets.AddRangeAsync(assets);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
    }

    // -- UpdateAssetWithProductNameAsync (raw SQL) ----------------------------

    /// <summary>
    /// Verifies that the raw SQL update renames the composite PK ProductName
    /// and persists all other field changes atomically.
    /// </summary>
    [Fact]
    public async Task UpdateAssetWithProductNameAsync_RenamesProductName_AndUpdatesFields()
    {
        await SeedAsync(MakeOrm("OldWallet", "alice@example.com", amount: 500m));

        var updated = Asset.Reconstitute(
            productName: "NewWallet",
            accountEmail: "alice@example.com",
            item: "FreeDepositAndWithdrawal",
            amount: 1500m,
            monetaryUnit: "KRW",
            note: "renamed",
            deleted: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        int rows = await _sut.UpdateAssetWithProductNameAsync(updated, "OldWallet", TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        Assert.Equal(1, rows);
        OrmAsset? result = await _context.Assets
            .FirstOrDefaultAsync(a => a.ProductName == "NewWallet" && a.AccountEmail == "alice@example.com",
                TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(1500m, result.Amount);
        Assert.Equal("renamed", result.Note);

        bool oldExists = await _context.Assets
            .AnyAsync(a => a.ProductName == "OldWallet", TestContext.Current.CancellationToken);
        Assert.False(oldExists);
    }

    /// <summary>
    /// Verifies that updating a non-existent ProductName affects zero rows.
    /// </summary>
    [Fact]
    public async Task UpdateAssetWithProductNameAsync_ReturnsZero_WhenProductNameNotFound()
    {
        await SeedAsync(MakeOrm("MyWallet", "alice@example.com"));

        var updated = Asset.Reconstitute(
            productName: "NewName",
            accountEmail: "alice@example.com",
            item: "FreeDepositAndWithdrawal",
            amount: 1000m,
            monetaryUnit: "KRW",
            note: null,
            deleted: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        int rows = await _sut.UpdateAssetWithProductNameAsync(updated, "DoesNotExist", TestContext.Current.CancellationToken);

        Assert.Equal(0, rows);
    }

    /// <summary>
    /// Verifies that the WHERE clause scopes the update to the correct AccountEmail,
    /// so another user's asset with the same ProductName is not affected.
    /// </summary>
    [Fact]
    public async Task UpdateAssetWithProductNameAsync_DoesNotAffect_OtherUsersAsset()
    {
        await SeedAsync(
            MakeOrm("Wallet", "alice@example.com"),
            MakeOrm("Wallet", "bob@example.com"));

        var updated = Asset.Reconstitute(
            productName: "AliceNewWallet",
            accountEmail: "alice@example.com",
            item: "FreeDepositAndWithdrawal",
            amount: 1000m,
            monetaryUnit: "KRW",
            note: null,
            deleted: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.UpdateAssetWithProductNameAsync(updated, "Wallet", TestContext.Current.CancellationToken);
        _context.ChangeTracker.Clear();

        bool bobUntouched = await _context.Assets
            .AnyAsync(a => a.ProductName == "Wallet" && a.AccountEmail == "bob@example.com",
                TestContext.Current.CancellationToken);
        Assert.True(bobUntouched);
    }

    // -- FindByEmailAndProductNameForUpdateAsync (SELECT ... FOR UPDATE) ------

    /// <summary>
    /// Verifies that the row-locking lookup finds an existing asset and returns null
    /// for a missing one (exercises the raw FOR UPDATE SQL against real PostgreSQL).
    /// </summary>
    [Fact]
    public async Task FindByEmailAndProductNameForUpdateAsync_FindsAsset_OrReturnsNull()
    {
        await SeedAsync(MakeOrm("LockedWallet", "alice@example.com", amount: 750m));

        Asset? found = await _sut.FindByEmailAndProductNameForUpdateAsync(
            "alice@example.com", "LockedWallet", TestContext.Current.CancellationToken);
        Asset? missing = await _sut.FindByEmailAndProductNameForUpdateAsync(
            "alice@example.com", "DoesNotExist", TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(750m, found.Balance.Amount);
        Assert.Null(missing);
    }

    /// <summary>
    /// Reproduces the double-submit race: two parallel transactions each read the same
    /// asset, deliberately overlap, then write back a withdrawn balance. Without
    /// FOR UPDATE the second write clobbers the first (lost update, final 900);
    /// with FOR UPDATE the reads serialize and both withdrawals survive (final 800).
    /// </summary>
    [Fact]
    public async Task FindByEmailAndProductNameForUpdateAsync_SerializesConcurrentBalanceUpdates()
    {
        await SeedAsync(MakeOrm("RaceWallet", "alice@example.com", amount: 1000m));

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        async Task WithdrawAsync(decimal amount)
        {
            await using var ctx = new ApplicationDbContext(options);
            var repo = new AssetRepository(ctx);
            await using var tx = await ctx.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

            Asset? asset = await repo.FindByEmailAndProductNameForUpdateAsync(
                "alice@example.com", "RaceWallet", TestContext.Current.CancellationToken);
            Assert.NotNull(asset);

            // Hold the row long enough that both tasks overlap inside the read-modify-write window.
            await Task.Delay(300, TestContext.Current.CancellationToken);

            asset.Withdraw(amount, asset.Balance.Currency);
            await repo.UpdateEntityAsync(asset, TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(WithdrawAsync(100m), WithdrawAsync(100m));

        _context.ChangeTracker.Clear();
        OrmAsset? result = await _context.Assets.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ProductName == "RaceWallet" && a.AccountEmail == "alice@example.com",
                TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(800m, result.Amount); // 1000 - 100 - 100: no lost update
    }
}
