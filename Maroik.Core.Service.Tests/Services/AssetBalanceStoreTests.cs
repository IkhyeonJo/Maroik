using Maroik.Core.Domain.Finances;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AssetBalanceStore"/>.
/// Pins the double-submit guard: balance reads must go through the row-locking
/// repository query (SELECT ... FOR UPDATE) so that concurrent balance mutations
/// of the same asset serialize inside the ambient unit-of-work transaction.
/// </summary>
public class AssetBalanceStoreTests
{
    /// <summary>Fake <c>IAssetRepository</c> injected into the system under test.</summary>
    private readonly FakeAssetRepository _assetRepo = new();

    /// <summary>The service under test over the fake repository.</summary>
    private AssetBalanceStore CreateSut() => new(_assetRepo);

    /// <summary>A persisted, active 1000 KRW asset named <paramref name="name"/>.</summary>
    private static Asset MakeAsset(string name = "Wallet") =>
        Asset.Reconstitute(name, "user@example.com", "FreeDepositAndWithdrawal", 1000m, "KRW", null, false, DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>Verifies that <c>GetAssetAsync</c> reads through the row-locking query, not the plain lookup.</summary>
    [Fact]
    public async Task GetAssetAsync_UsesRowLockingQuery()
    {
        var asset = MakeAsset();
        _assetRepo.AnswerFindByEmailAndProductNameForUpdateAsync((accountEmail, productName, _) => accountEmail == "user@example.com" && productName == "Wallet", (_, _, _) => Task.FromResult<Asset?>(asset));
        var sut = CreateSut();

        Asset? result = await sut.GetAssetAsync("user@example.com", "Wallet", TestContext.Current.CancellationToken);

        Assert.Same(asset, result);
        Assert.Single(_assetRepo.FindByEmailAndProductNameForUpdateAsyncCalls, call => call.AccountEmail == "user@example.com" && call.ProductName == "Wallet");
        Assert.Empty(_assetRepo.FindByEmailAndProductNameAsyncCalls);
    }

    /// <summary>Verifies that <c>GetAssetAsync</c> returns null when the asset does not exist.</summary>
    [Fact]
    public async Task GetAssetAsync_ReturnsNull_WhenAssetNotFound()
    {
        _assetRepo.AnswerFindByEmailAndProductNameForUpdateAsync((_, _, _) => Task.FromResult<Asset?>(null));
        var sut = CreateSut();

        Asset? result = await sut.GetAssetAsync("user@example.com", "Missing", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that <c>GetAssetsAsync</c> delegates to the batched row-locking repository query
    /// (a single round trip for every requested name, instead of one query per name) and keys the
    /// result by product name.
    /// </summary>
    [Fact]
    public async Task GetAssetsAsync_DelegatesToBatchedRowLockingQuery_AndKeysResultByProductName()
    {
        var wallet = MakeAsset();
        var savings = MakeAsset("Savings");
        _assetRepo.AnswerFindByEmailAndProductNamesForUpdateAsync((accountEmail, productNames, _) => accountEmail == "user@example.com" && (productNames.Count == 2 && productNames.Contains("Wallet") && productNames.Contains("Savings")), (_, _, _) => Task.FromResult<List<Asset>>([wallet, savings]));
        var sut = CreateSut();

        Dictionary<string, Asset> result = await sut.GetAssetsAsync(
            "user@example.com", ["Wallet", "Savings"], TestContext.Current.CancellationToken);

        Assert.Equal(2, result.Count);
        Assert.Same(wallet, result["Wallet"]);
        Assert.Same(savings, result["Savings"]);
        Assert.Single(_assetRepo.FindByEmailAndProductNamesForUpdateAsyncCalls);
    }

    /// <summary>Verifies that <c>SaveAsync</c> delegates to the repository update.</summary>
    [Fact]
    public async Task SaveAsync_DelegatesToRepositoryUpdate()
    {
        var asset = MakeAsset();
        var sut = CreateSut();

        await sut.SaveAsync(asset, TestContext.Current.CancellationToken);

        Assert.Single(_assetRepo.UpdateEntityAsyncCalls, call => call.Domain == asset);
    }
}
