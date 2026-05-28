using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Asset table.
/// Lookup/create/delete/update operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IAssetRepository : IGenericRepository<Asset>
{
    /// <summary>
    /// Renames the ProductName PK column via raw SQL (EF Core cannot update a PK via tracking).
    /// Returns the number of rows affected.
    /// </summary>
    Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName);
}
