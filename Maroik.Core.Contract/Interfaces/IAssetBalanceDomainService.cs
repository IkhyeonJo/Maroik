using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Domain service abstraction for reading and persisting <see cref="Asset"/> balance state.
/// Shields Finance transaction services from a direct repository dependency.
/// </summary>
public interface IAssetBalanceDomainService
{
    /// <summary>
    /// Returns the asset identified by the composite key (email + productName), or null if not found.
    /// The row is read with a database lock (SELECT ... FOR UPDATE): within an active
    /// <see cref="IUnitOfWork"/> transaction the asset is protected from concurrent balance
    /// mutations until commit/rollback, so double-submitted requests serialize instead of
    /// losing an update. Outside a transaction the lock is released at end of statement.
    /// </summary>
    Task<Asset?> GetAssetAsync(string accountEmail, string productName, CancellationToken ct = default);

    /// <summary>Persists changes to the given asset (insert or update).</summary>
    Task SaveAsync(Asset asset, CancellationToken ct = default);
}
