using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Domain service abstraction for reading and persisting <see cref="Asset"/> balance state.
/// Shields Finance transaction services from a direct repository dependency.
/// </summary>
public interface IAssetBalanceDomainService
{
    /// <summary>Returns the asset identified by the composite key (email + productName), or null if not found.</summary>
    Task<Asset?> GetAssetAsync(string accountEmail, string productName);

    /// <summary>Persists changes to the given asset (insert or update).</summary>
    Task SaveAsync(Asset asset);
}
