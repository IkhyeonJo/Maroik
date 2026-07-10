using Maroik.Core.Contract.Domains.Finance;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="Asset"/> persistence.
/// </summary>
public interface IAssetRepository : IGenericRepository<Asset>
{
    /// <summary>
    /// Updates an asset, matching the existing row by its original product name
    /// (used when the update itself renames the product). Returns the number of rows affected.
    /// </summary>
    Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName, CancellationToken ct = default);

    /// <summary>Returns all assets belonging to the given account.</summary>
    Task<List<Asset>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>Returns the asset identified by account email and product name, or null if not found.</summary>
    Task<Asset?> FindByEmailAndProductNameAsync(string accountEmail, string productName, CancellationToken ct = default);

    /// <summary>
    /// Same lookup as <see cref="FindByEmailAndProductNameAsync"/> but takes a row-level
    /// database lock (SELECT ... FOR UPDATE). Within an active <see cref="IUnitOfWork"/>
    /// transaction the lock is held until commit/rollback, serializing concurrent
    /// balance mutations of the same asset (e.g. double-submitted requests).
    /// </summary>
    Task<Asset?> FindByEmailAndProductNameForUpdateAsync(string accountEmail, string productName, CancellationToken ct = default);
}
