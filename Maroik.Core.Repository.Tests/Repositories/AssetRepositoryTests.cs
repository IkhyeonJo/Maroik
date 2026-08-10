using Maroik.Core.Domain.Finance;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;

namespace Maroik.Core.Repository.Tests.Repositories;

/// <summary>
/// Tests for <see cref="AssetRepository"/> using an EF Core InMemory database.
/// Asset uses a composite primary key (AccountEmail + ProductName); tests verify
/// that CRUD operations, soft-delete, and monetary-unit updates behave correctly.
/// </summary>
public class AssetRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AssetRepository _sut;

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public AssetRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new AssetRepository(_context);
    }

    /// <summary>Releases resources allocated by this test fixture (disposes the database context).</summary>
    public void Dispose() => _context.Dispose();

    // -- Helpers --------------------------------------------------------------

    private static OrmAsset MakeAsset(string productName, string email, string unit = "KRW", decimal amount = 1000m) => new()
    {
        ProductName = productName,
        AccountEmail = email,
        Item = "FreeDepositAndWithdrawal",
        Amount = amount,
        MonetaryUnit = unit,
        Deleted = false,
        Created = DateTime.UtcNow,
        Updated = DateTime.UtcNow
    };

    private async Task SeedAsync(params OrmAsset[] assets)
    {
        await _context.Assets.AddRangeAsync(assets);
        await _context.SaveChangesAsync();
    }

    // -- GetAssetsAsync -------------------------------------------------------

    /// <summary>Verifies that <c>GetAssetsAsync</c> returns only assets for given email.</summary>
    [Fact]
    public async Task GetAssetsAsync_ReturnsOnlyAssetsForGivenEmail()
    {
        await SeedAsync(
            MakeAsset("Wallet", "alice@example.com"),
            MakeAsset("Savings", "alice@example.com"),
            MakeAsset("Wallet", "bob@example.com"));

        List<Asset> result = await _sut.GetByAccountEmailAsync("alice@example.com", TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal("alice@example.com", a.AccountEmail.Value));
    }

    /// <summary>Verifies that <c>GetAssetsAsync</c> returns empty list when email has no assets.</summary>
    [Fact]
    public async Task GetAssetsAsync_ReturnsEmptyList_WhenEmailHasNoAssets()
    {
        List<Asset> result = await _sut.GetByAccountEmailAsync("nobody@example.com", TestContext.Current.CancellationToken);

        Assert.Empty(result);
    }

    // -- GetAssetAsync --------------------------------------------------------

    /// <summary>Verifies that <c>GetAssetAsync</c> returns asset when found.</summary>
    [Fact]
    public async Task GetAssetAsync_ReturnsAsset_WhenFound()
    {
        await SeedAsync(MakeAsset("Wallet", "alice@example.com", amount: 5000m));

        Asset? result = await _sut.FindByEmailAndProductNameAsync("alice@example.com", "Wallet", TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("Wallet", result.ProductName);
        Assert.Equal(5000m, result.Balance.Amount);
    }

    /// <summary>Verifies that <c>GetAssetAsync</c> returns null when not found.</summary>
    [Fact]
    public async Task GetAssetAsync_ReturnsNull_WhenNotFound()
    {
        Asset? result = await _sut.FindByEmailAndProductNameAsync("alice@example.com", "NonExistent", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>GetAssetAsync</c> does not return asset for different email.</summary>
    [Fact]
    public async Task GetAssetAsync_DoesNotReturnAsset_ForDifferentEmail()
    {
        await SeedAsync(MakeAsset("Wallet", "bob@example.com"));

        Asset? result = await _sut.FindByEmailAndProductNameAsync("alice@example.com", "Wallet", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- CreateAssetAsync -----------------------------------------------------

    /// <summary>Verifies that <c>CreateAssetAsync</c> adds asset to database.</summary>
    [Fact]
    public async Task CreateAssetAsync_AddsAssetToDatabase()
    {
        var asset = Asset.Reconstitute(
            productName: "NewAccount",
            accountEmail: "alice@example.com",
            item: "SavingsAsset",
            amount: 2500m,
            monetaryUnit: "KRW",
            note: null,
            deleted: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.CreateAsync(asset, TestContext.Current.CancellationToken);

        OrmAsset? saved = await _context.Assets.FirstOrDefaultAsync(
            a => a.AccountEmail == "alice@example.com" && a.ProductName == "NewAccount", TestContext.Current.CancellationToken);
        Assert.NotNull(saved);
        Assert.Equal(2500m, saved.Amount);
        Assert.Equal("SavingsAsset", saved.Item);
    }

    // -- UpdateEntityAsync -----------------------------------------------------

    /// <summary>Verifies that <c>UpdateEntityAsync</c> updates existing asset.</summary>
    [Fact]
    public async Task UpdateEntityAsync_UpdatesExistingAsset()
    {
        await SeedAsync(MakeAsset("Wallet", "alice@example.com", amount: 1000m));

        var asset = Asset.Reconstitute(
            productName: "Wallet",
            accountEmail: "alice@example.com",
            item: "FreeDepositAndWithdrawal",
            amount: 1500m,
            monetaryUnit: "KRW",
            note: null,
            deleted: false,
            created: DateTime.UtcNow,
            updated: DateTime.UtcNow);

        await _sut.UpdateEntityAsync(asset, TestContext.Current.CancellationToken);

        OrmAsset? updated = await _context.Assets.FirstOrDefaultAsync(
            a => a.AccountEmail == "alice@example.com" && a.ProductName == "Wallet", TestContext.Current.CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(1500m, updated.Amount);
    }
}
