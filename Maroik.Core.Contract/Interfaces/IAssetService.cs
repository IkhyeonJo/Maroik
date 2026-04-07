using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for asset management business logic.
/// </summary>
public interface IAssetService
{
    /// <summary>Returns all active assets for the given account.</summary>
    Task<List<AssetResponse>> GetAssetsAsync(string accountEmail);

    /// <summary>Returns a single asset by product name, or null.</summary>
    Task<AssetResponse?> GetAssetAsync(string accountEmail, string productName);

    /// <summary>Validates and creates a new asset for the given account.</summary>
    Task<ServiceResult> CreateAsync(string accountEmail, AssetRequest request);

    /// <summary>
    /// Updates an asset. If the product name changed, cascades the rename to linked
    /// income and expenditure records.
    /// </summary>
    Task<ServiceResult> UpdateAsync(string accountEmail, AssetRequest request, string originalProductName);

    /// <summary>Soft-deletes an asset (sets Deleted = true).</summary>
    Task<ServiceResult> DeleteAsync(string accountEmail, string productName);
}
