using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Extensions;

/// <summary>
/// Extension methods for locking multiple <see cref="Asset"/> rows through
/// <see cref="IAssetBalanceDomainService"/> in a deadlock-safe order.
/// </summary>
public static class AssetBalanceOrdinalLockExtensions
{
    /// <summary>
    /// Fetches (and row-locks via <see cref="IAssetBalanceDomainService.GetAssetAsync"/>) every
    /// distinct, non-empty asset name in <paramref name="productNames"/>, always in ordinal name
    /// order. Locking in a single, name-based order regardless of the caller's role for each asset
    /// (payment vs. deposit, previous vs. new, ...) ensures every transaction that touches the same
    /// pair of assets acquires their locks in the same order, so concurrent transactions cannot
    /// deadlock on each other.
    /// </summary>
    public static async Task<Dictionary<string, Asset>> GetAssetsOrdinalAsync(
        this IAssetBalanceDomainService assetBalance,
        string accountEmail,
        IEnumerable<string?> productNames,
        CancellationToken ct = default)
    {
        var assets = new Dictionary<string, Asset>(StringComparer.Ordinal);
        IEnumerable<string> names = productNames
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(n => n, StringComparer.Ordinal);

        foreach (string name in names)
        {
            Asset? asset = await assetBalance.GetAssetAsync(accountEmail, name, ct);
            if (asset != null) assets[name] = asset;
        }

        return assets;
    }
}
