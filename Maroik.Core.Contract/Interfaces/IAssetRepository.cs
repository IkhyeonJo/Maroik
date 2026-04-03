using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Contract.Interfaces;

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