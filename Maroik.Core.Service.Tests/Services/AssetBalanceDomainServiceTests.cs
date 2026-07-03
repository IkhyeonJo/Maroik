using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Service.Services;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="AssetBalanceDomainService"/>.
/// Pins the double-submit guard: balance reads must go through the row-locking
/// repository query (SELECT ... FOR UPDATE) so that concurrent balance mutations
/// of the same asset serialize inside the ambient unit-of-work transaction.
/// </summary>
public class AssetBalanceDomainServiceTests
{
    private readonly Mock<IAssetRepository> _assetRepo = new();

    private AssetBalanceDomainService CreateSut() => new(_assetRepo.Object);

    private static Asset MakeAsset(string name = "Wallet") =>
        Asset.Reconstitute(name, "user@example.com", "FreeDepositAndWithdrawal", 1000m, "KRW", null, false, DateTime.UtcNow, DateTime.UtcNow);

    /// <summary>Verifies that <c>GetAssetAsync</c> reads through the row-locking query, not the plain lookup.</summary>
    [Fact]
    public async Task GetAssetAsync_UsesRowLockingQuery()
    {
        var asset = MakeAsset();
        _assetRepo.Setup(r => r.FindByEmailAndProductNameForUpdateAsync("user@example.com", "Wallet", It.IsAny<CancellationToken>()))
            .ReturnsAsync(asset);
        var sut = CreateSut();

        Asset? result = await sut.GetAssetAsync("user@example.com", "Wallet", TestContext.Current.CancellationToken);

        Assert.Same(asset, result);
        _assetRepo.Verify(r => r.FindByEmailAndProductNameForUpdateAsync("user@example.com", "Wallet", It.IsAny<CancellationToken>()), Times.Once);
        _assetRepo.Verify(r => r.FindByEmailAndProductNameAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>GetAssetAsync</c> returns null when the asset does not exist.</summary>
    [Fact]
    public async Task GetAssetAsync_ReturnsNull_WhenAssetNotFound()
    {
        _assetRepo.Setup(r => r.FindByEmailAndProductNameForUpdateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Asset?)null);
        var sut = CreateSut();

        Asset? result = await sut.GetAssetAsync("user@example.com", "Missing", TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>Verifies that <c>SaveAsync</c> delegates to the repository update.</summary>
    [Fact]
    public async Task SaveAsync_DelegatesToRepositoryUpdate()
    {
        var asset = MakeAsset();
        var sut = CreateSut();

        await sut.SaveAsync(asset, TestContext.Current.CancellationToken);

        _assetRepo.Verify(r => r.UpdateEntityAsync(asset, It.IsAny<CancellationToken>()), Times.Once);
    }
}
