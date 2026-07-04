using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface IAssetRepository
{
    /// <summary>
    /// Gets all assets for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<Asset>> GetAssetsAsync(string email);

    /// <summary>
    /// Gets a specific asset for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="productName"></param>
    /// <returns></returns>
    public Task<Asset> GetAssetAsync(string email, string productName);

    /// <summary>
    /// Gets all assets for the logged-in account with a database row lock (SELECT ... FOR UPDATE).
    /// Must be called inside an active transaction; the locks are held until commit/rollback,
    /// so concurrent balance mutations for the same account are serialized (prevents lost updates).
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<Asset>> GetAssetsForUpdateAsync(string email);

    /// <summary>
    /// Creates an asset.
    /// </summary>
    /// <param name="asset"></param>
    /// <returns></returns>
    public Task CreateAssetAsync(Asset asset);

    /// <summary>
    /// Updates an asset.
    /// </summary>
    /// <param name="asset"></param>
    /// <returns></returns>
    public Task UpdateAssetAsync(Asset asset);

    /// <summary>
    /// Updates an asset including asset name.
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="originalProductName"></param>
    /// <returns></returns>
    public Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName);
}