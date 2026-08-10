using Maroik.Core.Domain.Finance;
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
    /// <remarks>
    /// Delegates to the row-locking lookup so that concurrent balance mutations of the
    /// same asset serialize inside the ambient unit-of-work transaction (lost-update guard).
    /// </remarks>
    public Task<Asset?> GetAssetAsync(string accountEmail, string productName, CancellationToken ct = default)
        => _assetRepository.FindByEmailAndProductNameForUpdateAsync(accountEmail, productName, ct);

    /// <inheritdoc />
    public Task SaveAsync(Asset asset, CancellationToken ct = default)
        => _assetRepository.UpdateEntityAsync(asset, ct);
}
