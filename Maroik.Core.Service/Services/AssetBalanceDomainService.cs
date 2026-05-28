using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Filters;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <summary>
/// Thin delegating wrapper that exposes only the asset-balance operations
/// needed by Finance transaction services, keeping them free of a direct
/// <see cref="IAssetRepository"/> dependency.
/// </summary>
public class AssetBalanceDomainService : IAssetBalanceDomainService
{
    private readonly IAssetRepository _assetRepository;

    /// <summary>Initializes a new instance of <see cref="AssetBalanceDomainService"/>.</summary>
    /// <param name="assetRepository">The repository used for asset read and update operations.</param>
    public AssetBalanceDomainService(IAssetRepository assetRepository)
        => _assetRepository = assetRepository;

    /// <inheritdoc />
    public async Task<Asset?> GetAssetAsync(string accountEmail, string productName)
        => (await _assetRepository.FindAsync([
            FilterCondition.Equal("AccountEmail", accountEmail),
            FilterCondition.Equal("ProductName", productName)
        ])).FirstOrDefault();

    /// <inheritdoc />
    public Task SaveAsync(Asset asset)
        => _assetRepository.UpdateEntityAsync(asset);
}
