using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for Asset table CRUD operations.
/// </summary>
public interface IAssetRepository
{
    /// <summary>Returns all non-deleted assets belonging to the given account email.</summary>
    public Task<List<Asset>> GetAssetsAsync(string email);

    /// <summary>Returns a single asset identified by the composite key (email + productName), or null.</summary>
    public Task<Asset?> GetAssetAsync(string email, string productName);

    /// <summary>Inserts a new asset row.</summary>
    public Task CreateAssetAsync(Asset asset);

    /// <summary>Updates an existing asset row (product name unchanged).</summary>
    public Task UpdateAssetAsync(Asset asset);

    /// <summary>
    /// Renames an asset's product name from <paramref name="originalProductName"/> to the value in
    /// <paramref name="asset"/>, cascading the rename to all linked records.
    /// Returns the number of rows affected.
    /// </summary>
    public Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName);

    /// <summary>
    /// Bulk-updates the MyDepositAsset field on all assets belonging to <paramref name="accountEmail"/>
    /// from <paramref name="originalProductName"/> to <paramref name="newProductName"/>.
    /// </summary>
    public Task UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string originalProductName, string newProductName);
}
