using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for Asset table CRUD operations.
/// </summary>
public interface IAssetRepository
{
    /// <summary>Returns all non-deleted assets belonging to the given account email.</summary>
    public Task<List<AssetResponse>> GetAssetsAsync(string email);

    /// <summary>Returns a single asset identified by the composite key (email + productName), or null.</summary>
    public Task<AssetResponse?> GetAssetAsync(string email, string productName);

    /// <summary>Inserts a new asset row.</summary>
    public Task CreateAssetAsync(AssetRequest request);

    /// <summary>Updates an existing asset row (product name unchanged).</summary>
    public Task UpdateAssetAsync(AssetRequest request);

    /// <summary>
    /// Renames an asset's product name from <paramref name="originalProductName"/> to the value in
    /// <paramref name="request"/>, and cascades the rename to all linked income/expenditure records.
    /// Returns the number of rows affected.
    /// </summary>
    public Task<int> UpdateAssetWithProductNameAsync(AssetRequest request, string originalProductName);
}
