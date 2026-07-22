using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Extensions;
using Moq;

namespace Maroik.Core.Service.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="AssetBalanceOrdinalLockExtensions.GetAssetsOrdinalAsync"/>.
/// Verifies the deadlock-avoidance contract: assets are always fetched/locked in
/// <see cref="StringComparer.Ordinal"/> order regardless of caller-supplied order, duplicate
/// names are only fetched once, blank/null names are skipped entirely, and assets that don't
/// exist are simply omitted from the result rather than stored as null.
/// </summary>
public class AssetBalanceOrdinalLockExtensionsTests
{
    private const string Email = "user@example.com";

    private readonly Mock<IAssetBalanceDomainService> _assetBalance = new();

    private static Asset MakeAsset(string productName) =>
        Asset.Reconstitute(productName, Email, "Deposit", 100m, "KRW", null, false, DateTime.UtcNow, DateTime.UtcNow);

    [Fact]
    public async Task GetAssetsOrdinalAsync_FetchesNames_InOrdinalOrder_RegardlessOfInputOrder()
    {
        var callOrder = new List<string>();
        _assetBalance.Setup(a => a.GetAssetAsync(Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, name, _) => callOrder.Add(name))
            .Returns<string, string, CancellationToken>((_, name, _) => Task.FromResult<Asset?>(MakeAsset(name)));

        // Deliberately scrambled/reverse-ordinal input order.
        string[] names = ["Zebra", "apple", "Mango"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        // Ordinal comparison: uppercase letters sort before lowercase, so "Mango" < "Zebra" < "apple".
        Assert.Equal(["Mango", "Zebra", "apple"], callOrder);
        Assert.Equal(3, result.Count);
        Assert.Equal("Mango", result["Mango"].ProductName);
        Assert.Equal("Zebra", result["Zebra"].ProductName);
        Assert.Equal("apple", result["apple"].ProductName);
    }

    [Fact]
    public async Task GetAssetsOrdinalAsync_FetchesDuplicateNames_OnlyOnce()
    {
        var callOrder = new List<string>();
        _assetBalance.Setup(a => a.GetAssetAsync(Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, name, _) => callOrder.Add(name))
            .Returns<string, string, CancellationToken>((_, name, _) => Task.FromResult<Asset?>(MakeAsset(name)));

        string[] names = ["Apple", "Apple", "Banana", "Apple"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        Assert.Equal(["Apple", "Banana"], callOrder);
        _assetBalance.Verify(a => a.GetAssetAsync(Email, "Apple", It.IsAny<CancellationToken>()), Times.Once);
        _assetBalance.Verify(a => a.GetAssetAsync(Email, "Banana", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAssetsOrdinalAsync_FiltersOutNullOrEmptyNames_WithoutCallingGetAssetAsync()
    {
        _assetBalance.Setup(a => a.GetAssetAsync(Email, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, string, CancellationToken>((_, name, _) => Task.FromResult<Asset?>(MakeAsset(name)));

        List<string?> names = ["Apple", null, "", "Banana"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        _assetBalance.Verify(a => a.GetAssetAsync(Email, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _assetBalance.Verify(a => a.GetAssetAsync(Email, null!, It.IsAny<CancellationToken>()), Times.Never);
        _assetBalance.Verify(a => a.GetAssetAsync(Email, "", It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("Apple"));
        Assert.True(result.ContainsKey("Banana"));
    }

    [Fact]
    public async Task GetAssetsOrdinalAsync_ExcludesName_WhenGetAssetAsyncReturnsNull()
    {
        _assetBalance.Setup(a => a.GetAssetAsync(Email, "Apple", It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeAsset("Apple"));
        _assetBalance.Setup(a => a.GetAssetAsync(Email, "Missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Asset?)null);

        string[] names = ["Apple", "Missing"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.True(result.ContainsKey("Apple"));
        Assert.False(result.ContainsKey("Missing"));
    }
}
