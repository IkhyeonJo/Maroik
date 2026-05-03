using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Domain service abstraction for reading and persisting <see cref="Asset"/> balance state.
/// Shields Finance transaction services from a direct repository dependency.
/// </summary>
public interface IAssetBalanceDomainService
{
    Task<Asset?> GetAssetAsync(string accountEmail, string productName);
    Task SaveAsync(Asset asset);
}
