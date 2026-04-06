using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IAssetService
{
    Task<List<AssetResponse>> GetAssetsAsync(string accountEmail);
    Task<AssetResponse?> GetAssetAsync(string accountEmail, string productName);
    Task<ServiceResult> CreateAsync(string accountEmail, AssetRequest request);
    Task<ServiceResult> UpdateAsync(string accountEmail, AssetRequest request, string originalProductName);
    Task<ServiceResult> DeleteAsync(string accountEmail, string productName);
}
