using Maroik.Core.Contract.Dtos;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Maroik.Core.Repository.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Tests.Repositories;

public class AssetRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AssetRepository _sut;

    public AssetRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _sut = new AssetRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Asset MakeAsset(string productName, string email, string unit = "KRW", decimal amount = 1000m) => new()
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

    private async Task SeedAsync(params Asset[] assets)
    {
        await _context.Assets.AddRangeAsync(assets);
        await _context.SaveChangesAsync();
    }

    // ── GetAssetsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetAssetsAsync_ReturnsOnlyAssetsForGivenEmail()
    {
        await SeedAsync(
            MakeAsset("Wallet", "alice@example.com"),
            MakeAsset("Savings", "alice@example.com"),
            MakeAsset("Wallet", "bob@example.com"));

        List<AssetResponse> result = await _sut.GetAssetsAsync("alice@example.com");

        Assert.Equal(2, result.Count);
        Assert.All(result, a => Assert.Equal("alice@example.com", a.AccountEmail));
    }

    [Fact]
    public async Task GetAssetsAsync_ReturnsEmptyList_WhenEmailHasNoAssets()
    {
        List<AssetResponse> result = await _sut.GetAssetsAsync("nobody@example.com");

        Assert.Empty(result);
    }

    // ── GetAssetAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAssetAsync_ReturnsAsset_WhenFound()
    {
        await SeedAsync(MakeAsset("Wallet", "alice@example.com", amount: 5000m));

        AssetResponse? result = await _sut.GetAssetAsync("alice@example.com", "Wallet");

        Assert.NotNull(result);
        Assert.Equal("Wallet", result.ProductName);
        Assert.Equal(5000m, result.Amount);
    }

    [Fact]
    public async Task GetAssetAsync_ReturnsNull_WhenNotFound()
    {
        AssetResponse? result = await _sut.GetAssetAsync("alice@example.com", "NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAssetAsync_DoesNotReturnAsset_ForDifferentEmail()
    {
        await SeedAsync(MakeAsset("Wallet", "bob@example.com"));

        AssetResponse? result = await _sut.GetAssetAsync("alice@example.com", "Wallet");

        Assert.Null(result);
    }

    // ── CreateAssetAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAssetAsync_AddsAssetToDatabase()
    {
        var request = new AssetRequest
        {
            ProductName = "NewAccount",
            AccountEmail = "alice@example.com",
            Item = "SavingsAsset",
            Amount = 2500m,
            MonetaryUnit = "KRW",
            Deleted = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        await _sut.CreateAssetAsync(request);

        Asset? saved = await _context.Assets.FirstOrDefaultAsync(
            a => a.AccountEmail == "alice@example.com" && a.ProductName == "NewAccount");
        Assert.NotNull(saved);
        Assert.Equal(2500m, saved.Amount);
        Assert.Equal("SavingsAsset", saved.Item);
    }

    // ── UpdateAssetAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAssetAsync_UpdatesExistingAsset()
    {
        await SeedAsync(MakeAsset("Wallet", "alice@example.com", amount: 1000m));

        var request = new AssetRequest
        {
            ProductName = "Wallet",
            AccountEmail = "alice@example.com",
            Item = "FreeDepositAndWithdrawal",
            Amount = 1500m,
            MonetaryUnit = "KRW",
            Deleted = false,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow
        };

        await _sut.UpdateAssetAsync(request);

        Asset? updated = await _context.Assets.FirstOrDefaultAsync(
            a => a.AccountEmail == "alice@example.com" && a.ProductName == "Wallet");
        Assert.NotNull(updated);
        Assert.Equal(1500m, updated.Amount);
    }
}
