using Maroik.Core.Contract.Domains.Finance;
namespace Maroik.Core.Contract.Interfaces;

public interface IAssetRepository : IGenericRepository<Asset>
{
    Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName, CancellationToken ct = default);
    Task<List<Asset>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);
    Task<Asset?> FindByEmailAndProductNameAsync(string accountEmail, string productName, CancellationToken ct = default);

    /// <summary>
    /// Same lookup as <see cref="FindByEmailAndProductNameAsync"/> but takes a row-level
    /// database lock (SELECT ... FOR UPDATE). Within an active <see cref="IUnitOfWork"/>
    /// transaction the lock is held until commit/rollback, serializing concurrent
    /// balance mutations of the same asset (e.g. double-submitted requests).
    /// </summary>
    Task<Asset?> FindByEmailAndProductNameForUpdateAsync(string accountEmail, string productName, CancellationToken ct = default);
}
