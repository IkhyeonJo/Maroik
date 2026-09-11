using ErrorOr;
using Maroik.Core.Domain.Finance;
using Maroik.Core.Domain.ValueObjects;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Extensions;

/// <summary>
/// A single asset-balance adjustment to apply as part of reverting or re-applying the effect
/// a transaction record (income, expenditure, ...) had on an asset's balance.
/// </summary>
/// <param name="AssetName">Asset product name; empty/null adjustments are skipped.</param>
/// <param name="Amount">Amount to deposit or withdraw (sign is ignored — <see cref="IsDeposit"/> controls direction).</param>
/// <param name="IsDeposit">true to deposit into the asset, false to withdraw from it.</param>
/// <param name="Currency">
/// Currency the amount was <em>recorded</em> in (from the transaction row, not the asset's current
/// balance). Null falls back to the asset's current currency. When the asset's currency was changed
/// after the transaction was recorded, the two differ and the deposit/withdraw is rejected with a
/// currency-mismatch error instead of silently drifting the balance by a re-labelled amount.
/// </param>
public readonly record struct AssetBalanceAdjustment(string? AssetName, decimal Amount, bool IsDeposit, string? Currency = null);

/// <summary>
/// Extension methods for locking multiple <see cref="Asset"/> rows through
/// <see cref="IAssetBalanceDomainService"/> in a deadlock-safe order.
/// </summary>
public static class AssetBalanceOrdinalLockExtensions
{
    extension(IAssetBalanceDomainService assetBalance)
    {
        /// <summary>
        /// Fetches (and row-locks via <see cref="IAssetBalanceDomainService.GetAssetsAsync"/>, in a
        /// single batched round trip) every distinct, non-empty asset name in
        /// <paramref name="productNames"/>, always in ordinal name order. Locking in a single,
        /// name-based order regardless of the caller's role for each asset (payment vs. deposit,
        /// previous vs. new, ...) ensures every transaction that touches the same pair of assets
        /// acquires their locks in the same order, so concurrent transactions cannot deadlock on
        /// each other.
        /// </summary>
        public async Task<Dictionary<string, Asset>> GetAssetsOrdinalAsync(
            string accountEmail,
            IEnumerable<string?> productNames,
            CancellationToken ct = default)
        {
            List<string> names = [.. productNames
                .Where(n => !string.IsNullOrEmpty(n))
                .Select(n => n!)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(n => n, StringComparer.Ordinal)];

            return await assetBalance.GetAssetsAsync(accountEmail, names, ct);
        }

        /// <summary>
        /// Batch-fetches (via <see cref="GetAssetsOrdinalAsync"/>) every asset referenced by
        /// <paramref name="adjustments"/>, applies each deposit/withdrawal in order, then saves every
        /// touched asset. Used to revert a transaction record's old asset impact and/or apply its new
        /// one from a single fetched snapshot, avoiding duplicate reads of the same asset.
        /// </summary>
        /// <param name="adjustments">Adjustments to apply; entries with an empty <see cref="AssetBalanceAdjustment.AssetName"/> are skipped.</param>
        /// <param name="notFoundErrorCode">Error code used when a non-empty asset name does not resolve to an asset.</param>
        /// <param name="notFoundErrorMessage">Error message used when a non-empty asset name does not resolve to an asset.</param>
        public async Task<ErrorOr<Success>> ApplyAssetBalanceAdjustmentsAsync(
            string accountEmail,
            IEnumerable<AssetBalanceAdjustment> adjustments,
            string notFoundErrorCode,
            string notFoundErrorMessage,
            CancellationToken ct = default)
        {
            List<AssetBalanceAdjustment> toApply = [.. adjustments.Where(a => !string.IsNullOrEmpty(a.AssetName))];
            Dictionary<string, Asset> assets = await assetBalance.GetAssetsOrdinalAsync(
                accountEmail, toApply.Select(a => a.AssetName), ct);

            foreach (var adjustment in toApply)
            {
                if (!assets.TryGetValue(adjustment.AssetName!, out var asset))
                    return Error.NotFound(notFoundErrorCode, notFoundErrorMessage);

                Money amount;
                if (string.IsNullOrEmpty(adjustment.Currency))
                {
                    amount = asset.Balance.WithAmount(Math.Abs(adjustment.Amount));
                }
                else
                {
                    var moneyResult = Money.Create(Math.Abs(adjustment.Amount), adjustment.Currency);
                    if (moneyResult.IsError) return moneyResult.Errors;
                    amount = moneyResult.Value;
                }

                var result = adjustment.IsDeposit ? asset.Deposit(amount) : asset.Withdraw(amount);
                if (result.IsError) return result.Errors;
            }

            foreach (var asset in assets.Values)
                await assetBalance.SaveAsync(asset, ct);

            return Result.Success;
        }
    }
}
