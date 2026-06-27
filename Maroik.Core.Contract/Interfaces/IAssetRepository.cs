using Maroik.Core.Contract.Domains.Finance;
namespace Maroik.Core.Contract.Interfaces;

public interface IAssetRepository : IGenericRepository<Asset>
{
    Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName, CancellationToken ct = default);
    Task<List<Asset>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);
    Task<Asset?> FindByEmailAndProductNameAsync(string accountEmail, string productName, CancellationToken ct = default);
}
