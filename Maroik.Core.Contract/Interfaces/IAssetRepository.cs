using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IAssetRepository
{
    public Task<List<AssetResponse>> GetAssetsAsync(string email);
    public Task<AssetResponse?> GetAssetAsync(string email, string productName);
    public Task CreateAssetAsync(AssetRequest request);
    public Task UpdateAssetAsync(AssetRequest request);
    public Task<int> UpdateAssetWithProductNameAsync(AssetRequest request, string originalProductName);
}
