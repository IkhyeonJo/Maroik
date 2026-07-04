using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _context;

    public AssetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all assets for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<Asset>> GetAssetsAsync(string email)
    {
        return await _context.Assets.Where(x => x.AccountEmail == email).ToListAsync();
    }

    /// <summary>
    /// Gets a specific asset for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="productName"></param>
    /// <returns></returns>
    public async Task<Asset> GetAssetAsync(string email, string productName)
    {
        return await _context.Assets.FirstOrDefaultAsync(a => a.AccountEmail == email && a.ProductName == productName);
    }

    /// <summary>
    /// Gets all assets for the logged-in account with a database row lock (SELECT ... FOR UPDATE).
    /// Must be called inside an active transaction; the locks are held until commit/rollback.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<Asset>> GetAssetsForUpdateAsync(string email)
    {
        // Raw SQL is required because EF Core LINQ cannot emit FOR UPDATE.
        // ToListAsync on the raw root executes the SQL verbatim (no composition),
        // keeping FOR UPDATE at the top level of the statement.
        // ORDER BY "ProductName" makes the row-lock acquisition order deterministic,
        // so two concurrent transactions on the same account cannot deadlock.
        return await _context.Assets.FromSqlInterpolated(
            $"""
             SELECT *
             FROM "Asset"
             WHERE "AccountEmail" = {email}
             ORDER BY "ProductName"
             FOR UPDATE
             """).ToListAsync();
    }

    /// <summary>
    /// Creates an asset.
    /// </summary>
    /// <param name="asset"></param>
    /// <returns></returns>
    public async Task CreateAssetAsync(Asset asset)
    {
        _ = await _context.Assets.AddAsync(asset);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an asset.
    /// </summary>
    /// <param name="asset"></param>
    /// <returns></returns>
    public async Task UpdateAssetAsync(Asset asset)
    {
        _ = _context.Assets.Update(asset);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an asset including asset name.
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="originalProductName"></param>
    /// <returns></returns>
    public async Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "Asset"
             SET 
                 "ProductName" = {asset.ProductName},
                 "Item" = {asset.Item},
                 "Amount" = {asset.Amount},
                 "MonetaryUnit" = {asset.MonetaryUnit},
                 "Note" = {asset.Note},
                 "Deleted" = {asset.Deleted},
                 "Updated" = {asset.Updated}
             WHERE 
                 "ProductName" = {originalProductName}
                 AND "AccountEmail" = {asset.AccountEmail}
             """
        );
    }
}