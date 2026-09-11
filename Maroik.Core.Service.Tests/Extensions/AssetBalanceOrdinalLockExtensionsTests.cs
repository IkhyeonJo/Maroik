using Maroik.Core.Domain.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Extensions;
using Moq;

namespace Maroik.Core.Service.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="AssetBalanceOrdinalLockExtensions.GetAssetsOrdinalAsync"/>.
/// Verifies the deadlock-avoidance contract: assets are always requested from the batched
/// row-locking query in <see cref="StringComparer.Ordinal"/> order regardless of caller-supplied
/// order, duplicate names are only requested once, and blank/null names are filtered out before
/// the single batched call — all in one round trip rather than one query per name.
/// </summary>
public class AssetBalanceOrdinalLockExtensionsTests
{
    private const string Email = "user@example.com";

    private readonly Mock<IAssetBalanceDomainService> _assetBalance = new();

    private static Asset MakeAsset(string productName) =>
        Asset.Reconstitute(productName, Email, "Deposit", 100m, "KRW", null, false, DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>Get assets ordinal async requests names in ordinal order, in a single batched call, regardless of input order.</summary>
    [Fact]
    public async Task GetAssetsOrdinalAsync_RequestsNames_InOrdinalOrder_InASingleBatchedCall()
    {
        List<string>? requestedNames = null;
        _assetBalance.Setup(a => a.GetAssetsAsync(Email, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyCollection<string>, CancellationToken>((_, names, _) => requestedNames = [.. names])
            .ReturnsAsync((string _, IReadOnlyCollection<string> names, CancellationToken _) =>
                names.ToDictionary(n => n, MakeAsset, StringComparer.Ordinal));

        // Deliberately scrambled/reverse-ordinal input order.
        string[] names = ["Zebra", "apple", "Mango"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        // Ordinal comparison: uppercase letters sort before lowercase, so "Mango" < "Zebra" < "apple".
        Assert.Equal(["Mango", "Zebra", "apple"], requestedNames);
        _assetBalance.Verify(a => a.GetAssetsAsync(Email, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(3, result.Count);
        Assert.Equal("Mango", result["Mango"].ProductName);
        Assert.Equal("Zebra", result["Zebra"].ProductName);
        Assert.Equal("apple", result["apple"].ProductName);
    }

    /// <summary>Get assets ordinal async requests duplicate names only once, in the batched call.</summary>
    [Fact]
    public async Task GetAssetsOrdinalAsync_RequestsDuplicateNames_OnlyOnce()
    {
        List<string>? requestedNames = null;
        _assetBalance.Setup(a => a.GetAssetsAsync(Email, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyCollection<string>, CancellationToken>((_, names, _) => requestedNames = [.. names])
            .ReturnsAsync((string _, IReadOnlyCollection<string> names, CancellationToken _) =>
                names.ToDictionary(n => n, MakeAsset, StringComparer.Ordinal));

        string[] names = ["Apple", "Apple", "Banana", "Apple"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        Assert.Equal(["Apple", "Banana"], requestedNames);
        Assert.Equal(2, result.Count);
    }

    /// <summary>Get assets ordinal async filters out null or empty names before the batched call.</summary>
    [Fact]
    public async Task GetAssetsOrdinalAsync_FiltersOutNullOrEmptyNames_BeforeTheBatchedCall()
    {
        List<string>? requestedNames = null;
        _assetBalance.Setup(a => a.GetAssetsAsync(Email, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .Callback<string, IReadOnlyCollection<string>, CancellationToken>((_, names, _) => requestedNames = [.. names])
            .ReturnsAsync((string _, IReadOnlyCollection<string> names, CancellationToken _) =>
                names.ToDictionary(n => n, MakeAsset, StringComparer.Ordinal));

        List<string?> names = ["Apple", null, "", "Banana"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        Assert.Equal(["Apple", "Banana"], requestedNames);
        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("Apple"));
        Assert.True(result.ContainsKey("Banana"));
    }

    /// <summary>Get assets ordinal async passes through whatever the batched call returns, including a name with no match.</summary>
    [Fact]
    public async Task GetAssetsOrdinalAsync_ExcludesName_WhenBatchedCallOmitsIt()
    {
        _assetBalance.Setup(a => a.GetAssetsAsync(Email, It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, Asset>(StringComparer.Ordinal) { ["Apple"] = MakeAsset("Apple") });

        string[] names = ["Apple", "Missing"];

        Dictionary<string, Asset> result = await _assetBalance.Object.GetAssetsOrdinalAsync(
            Email, names, TestContext.Current.CancellationToken);

        Assert.Single(result);
        Assert.True(result.ContainsKey("Apple"));
        Assert.False(result.ContainsKey("Missing"));
    }
}
