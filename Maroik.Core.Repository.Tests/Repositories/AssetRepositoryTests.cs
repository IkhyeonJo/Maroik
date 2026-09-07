using Maroik.Core.Domain.Finance;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="AssetRepository"/> against a real PostgreSQL database (Testcontainers)
/// pre-loaded with the production <c>Init.sql</c> schema and seed data. <c>Asset</c> has a
/// composite primary key (<c>ProductName</c> + <c>AccountEmail</c>) and a foreign key to
/// <c>Account</c>; these tests exercise CRUD plus the two raw-SQL paths the EF Core InMemory
/// provider could not run — <c>UpdateAssetWithProductNameAsync</c> (renames a composite PK) and
/// <c>FindByEmailAndProductNameForUpdateAsync</c> (<c>SELECT ... FOR UPDATE</c>).
/// Every test scopes its data to <see cref="RepositoryTestBase.UniqueEmail"/> so it never touches
/// the seed's assets or a sibling test's.
/// </summary>
public sealed class AssetRepositoryTests(DatabaseFixture database) : RepositoryTestBase(database)
{
    private AssetRepository Sut => new(Context);

    // -- Helpers --------------------------------------------------------------

    private static OrmAsset MakeAsset(
        string productName, string email, string item = "FreeDepositAndWithdrawal",
        decimal amount = 1000m, string unit = "KRW", bool deleted = false) => new()
    {
        ProductName = productName,
        AccountEmail = email,
        Item = item,
        Amount = amount,
        MonetaryUnit = unit,
        Note = "",
        Deleted = deleted,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    /// <summary>Inserts the owning accounts (FK <c>Asset_fk_0</c>) then the assets.</summary>
    private async Task SeedAsync(params OrmAsset[] assets)
    {
        await EnsureAccountsAsync([.. assets.Select(a => a.AccountEmail!)]);
        await Context.Assets.AddRangeAsync(assets);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
    }

    // -- GetByAccountEmailAsync ---------------------------------------------------

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns only assets for the given e-mail.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsOnlyAssetsForGivenEmail()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        await SeedAsync(
            MakeAsset("Wallet", alice),
            MakeAsset("Savings", alice),
            MakeAsset("Wallet", bob));

        List<Asset> result = await Sut.GetByAccountEmailAsync(alice, TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal(alice, a.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetByAccountEmailAsync</c> returns an empty list when the e-mail has no assets.</summary>
    [Fact]
    public async Task GetByAccountEmailAsync_ReturnsEmptyList_WhenEmailHasNoAssets()
    {
        List<Asset> result = await Sut.GetByAccountEmailAsync(UniqueEmail("nobody"), TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- FindByEmailAndProductNameAsync -------------------------------------------

    /// <summary>Verifies that <c>FindByEmailAndProductNameAsync</c> returns the asset when found.</summary>
    [Fact]
    public async Task FindByEmailAndProductNameAsync_ReturnsAsset_WhenFound()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("Wallet", alice, amount: 5000m));

        Asset? result = await Sut.FindByEmailAndProductNameAsync(alice, "Wallet", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Wallet", result.ProductName);
        Assert.Equal(5000m, result.Balance.Amount);
    }

    /// <summary>Verifies that <c>FindByEmailAndProductNameAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task FindByEmailAndProductNameAsync_ReturnsNull_WhenNotFound()
    {
        Asset? result = await Sut.FindByEmailAndProductNameAsync(
            UniqueEmail("alice"), "NonExistent", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>FindByEmailAndProductNameAsync</c> scopes by e-mail (composite PK).</summary>
    [Fact]
    public async Task FindByEmailAndProductNameAsync_DoesNotReturnAsset_ForDifferentEmail()
    {
        string bob = UniqueEmail("bob");
        await SeedAsync(MakeAsset("Wallet", bob));

        Asset? result = await Sut.FindByEmailAndProductNameAsync(
            UniqueEmail("alice"), "Wallet", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- CreateAsync --------------------------------------------------------------

    /// <summary>Verifies that <c>CreateAsync</c> inserts a new asset row.</summary>
    [Fact]
    public async Task CreateAsync_AddsAssetToDatabase()
    {
        string alice = UniqueEmail("alice");
        await EnsureAccountsAsync(alice);

        var asset = Asset.Reconstitute(
            productName: "NewAccount", accountEmail: alice, item: "SavingsAsset",
            amount: 2500m, monetaryUnit: "KRW", note: null, deleted: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Sut.CreateAsync(asset, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        OrmAsset? saved = await Context.Assets.FirstOrDefaultAsync(
            a => a.AccountEmail == alice && a.ProductName == "NewAccount", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(2500m, saved.Amount);
        Assert.Equal("SavingsAsset", saved.Item);
    }

    /// <summary>PostgreSQL enforces the <c>Asset_fk_0</c> foreign key — an asset for an unknown account throws (InMemory did not).</summary>
    [Fact]
    public async Task CreateAsync_Throws_WhenOwningAccountDoesNotExist()
    {
        var asset = Asset.Reconstitute(
            productName: "Orphan", accountEmail: UniqueEmail("ghost"), item: "CashAsset",
            amount: 1m, monetaryUnit: "KRW", note: null, deleted: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Assert.ThrowsAsync<DbUpdateException>(
            () => Sut.CreateAsync(asset, TestContext.Current.CancellationToken));
    }

    // -- UpdateEntityAsync ---------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> persists changes to an existing asset.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingAsset()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("Wallet", alice, amount: 1000m));

        var asset = Asset.Reconstitute(
            productName: "Wallet", accountEmail: alice, item: "FreeDepositAndWithdrawal",
            amount: 1500m, monetaryUnit: "KRW", note: "changed", deleted: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Sut.UpdateEntityAsync(asset, TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        OrmAsset? updated = await Context.Assets.FirstOrDefaultAsync(
            a => a.AccountEmail == alice && a.ProductName == "Wallet", TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(1500m, updated.Amount);
        Assert.Equal("changed", updated.Note);
    }

    // -- UpdateAssetWithProductNameAsync (raw SQL rename of a composite PK) --------

    /// <summary>Verifies that the raw SQL update renames the composite-PK <c>ProductName</c> and persists other fields atomically.</summary>
    [Fact]
    public async Task UpdateAssetWithProductNameAsync_RenamesProductName_AndUpdatesFields()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("OldWallet", alice, amount: 500m));

        var updated = Asset.Reconstitute(
            productName: "NewWallet", accountEmail: alice, item: "FreeDepositAndWithdrawal",
            amount: 1500m, monetaryUnit: "KRW", note: "renamed", deleted: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        int rows = await Sut.UpdateAssetWithProductNameAsync(updated, "OldWallet", TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.Equal(1, rows);
        OrmAsset? result = await Context.Assets.FirstOrDefaultAsync(
            a => a.ProductName == "NewWallet" && a.AccountEmail == alice, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(1500m, result.Amount);
        Assert.Equal("renamed", result.Note);
        Assert.False(await Context.Assets.AnyAsync(
            a => a.ProductName == "OldWallet" && a.AccountEmail == alice, TestContext.Current.CancellationToken));
    }

    /// <summary>Verifies that renaming a non-existent <c>ProductName</c> affects zero rows.</summary>
    [Fact]
    public async Task UpdateAssetWithProductNameAsync_ReturnsZero_WhenProductNameNotFound()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("MyWallet", alice));

        var updated = Asset.Reconstitute(
            productName: "NewName", accountEmail: alice, item: "FreeDepositAndWithdrawal",
            amount: 1000m, monetaryUnit: "KRW", note: null, deleted: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        int rows = await Sut.UpdateAssetWithProductNameAsync(updated, "DoesNotExist", TestContext.Current.CancellationToken);

        Assert.Equal(0, rows);
    }

    /// <summary>Verifies that the WHERE clause scopes the rename to the correct <c>AccountEmail</c>.</summary>
    [Fact]
    public async Task UpdateAssetWithProductNameAsync_DoesNotAffect_OtherUsersAsset()
    {
        string alice = UniqueEmail("alice");
        string bob = UniqueEmail("bob");
        await SeedAsync(MakeAsset("Wallet", alice), MakeAsset("Wallet", bob));

        var updated = Asset.Reconstitute(
            productName: "AliceNewWallet", accountEmail: alice, item: "FreeDepositAndWithdrawal",
            amount: 1000m, monetaryUnit: "KRW", note: null, deleted: false,
            created: DateTime.UtcNow, updated: DateTime.UtcNow);

        await Sut.UpdateAssetWithProductNameAsync(updated, "Wallet", TestContext.Current.CancellationToken);
        Context.ChangeTracker.Clear();

        Assert.True(await Context.Assets.AnyAsync(
            a => a.ProductName == "Wallet" && a.AccountEmail == bob, TestContext.Current.CancellationToken));
    }

    // -- FindByEmailAndProductNameForUpdateAsync (SELECT ... FOR UPDATE) ------

    /// <summary>Verifies the row-locking lookup finds an existing asset and returns null for a missing one.</summary>
    [Fact]
    public async Task FindByEmailAndProductNameForUpdateAsync_FindsAsset_OrReturnsNull()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("LockedWallet", alice, amount: 750m));

        Asset? found = await Sut.FindByEmailAndProductNameForUpdateAsync(alice, "LockedWallet", TestContext.Current.CancellationToken);
        Asset? missing = await Sut.FindByEmailAndProductNameForUpdateAsync(alice, "DoesNotExist", TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(750m, found.Balance.Amount);
        Assert.Null(missing);
    }

    /// <summary>
    /// A soft-deleted asset is still returned (not filtered) — callers rely on this to tell
    /// "doesn't exist" from "exists but deleted" (see IncomeService/ExpenditureService's explicit
    /// <c>.Deleted</c> checks after this call).
    /// </summary>
    [Fact]
    public async Task FindByEmailAndProductNameForUpdateAsync_ReturnsDeletedAsset_NotFiltered()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("RemovedWallet", alice, deleted: true));

        Asset? found = await Sut.FindByEmailAndProductNameForUpdateAsync(alice, "RemovedWallet", TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.True(found.Deleted);
    }

    /// <summary>
    /// Reproduces the double-submit race: two parallel transactions each read the same asset,
    /// overlap, then write back a withdrawn balance. Without FOR UPDATE the second write clobbers
    /// the first (final 900); with it the reads serialize and both withdrawals survive (final 800).
    /// </summary>
    [Fact]
    public async Task FindByEmailAndProductNameForUpdateAsync_SerializesConcurrentBalanceUpdates()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("RaceWallet", alice, amount: 1000m));

        ApplicationDbContext[] contexts = [NewDbContext(), NewDbContext()];

        async Task WithdrawAsync(ApplicationDbContext context, decimal amount)
        {
            var repo = new AssetRepository(context);
            await using var tx = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);

            Asset? asset = await repo.FindByEmailAndProductNameForUpdateAsync(alice, "RaceWallet", TestContext.Current.CancellationToken);
            Assert.NotNull(asset);

            await Task.Delay(300, TestContext.Current.CancellationToken);

            asset.Withdraw(asset.Balance.WithAmount(amount));
            await repo.UpdateEntityAsync(asset, TestContext.Current.CancellationToken);
            await tx.CommitAsync(TestContext.Current.CancellationToken);
        }

        await Task.WhenAll(WithdrawAsync(contexts[0], 100m), WithdrawAsync(contexts[1], 100m));

        Context.ChangeTracker.Clear();
        OrmAsset? result = await Context.Assets.AsNoTracking().FirstOrDefaultAsync(
            a => a.ProductName == "RaceWallet" && a.AccountEmail == alice, TestContext.Current.CancellationToken);
        Assert.NotNull(result);
        Assert.Equal(800m, result.Amount); // 1000 - 100 - 100: no lost update
    }

    /// <summary>
    /// Regression: a tracking query performs identity resolution, so if this asset's row is already
    /// tracked in the context (as ExpenditureService/IncomeService leave it via an earlier Include),
    /// a second tracking query would hand back the stale tracked instance instead of the row the
    /// FOR UPDATE just re-read. Asserts the method returns the true current value regardless.
    /// </summary>
    [Fact]
    public async Task FindByEmailAndProductNameForUpdateAsync_ReturnsFreshValue_WhenSameRowAlreadyTrackedInThisContext()
    {
        string alice = UniqueEmail("alice");
        await SeedAsync(MakeAsset("Wallet", alice, amount: 1000m));

        OrmAsset tracked = await Context.Assets.FirstAsync(
            a => a.ProductName == "Wallet" && a.AccountEmail == alice, TestContext.Current.CancellationToken);
        Assert.Equal(1000m, tracked.Amount);

        await using (var other = NewDbContext())
        {
            OrmAsset row = await other.Assets.FirstAsync(
                a => a.ProductName == "Wallet" && a.AccountEmail == alice, TestContext.Current.CancellationToken);
            row.Amount = 900m;
            await other.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        Asset? found = await Sut.FindByEmailAndProductNameForUpdateAsync(alice, "Wallet", TestContext.Current.CancellationToken);

        Assert.NotNull(found);
        Assert.Equal(900m, found.Balance.Amount);
    }
}
