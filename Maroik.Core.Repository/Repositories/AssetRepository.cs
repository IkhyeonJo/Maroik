using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IAssetRepository"/>.</summary>
public class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="AssetRepository"/> with <paramref name="context"/>.</summary>
    public AssetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<Asset>> GetAssetsAsync(string email)
        => (await _context.Assets.Where(x => x.AccountEmail == email).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<Asset?> GetAssetAsync(string email, string productName)
    {
        OrmAsset? entity = await _context.Assets.FirstOrDefaultAsync(a => a.AccountEmail == email && a.ProductName == productName);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task CreateAssetAsync(Asset asset)
    {
        _ = await _context.Assets.AddAsync(ToEntity(asset));
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAssetAsync(Asset asset)
    {
        _ = _context.Assets.Update(ToEntity(asset));
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "Asset"
             SET
                 "ProductName" = {asset.ProductName},
                 "Item" = {asset.Item},
                 "Amount" = {asset.Balance.Amount},
                 "MonetaryUnit" = {asset.Balance.Currency},
                 "Note" = {asset.Note},
                 "Deleted" = {asset.Deleted},
                 "Updated" = {asset.Updated}
             WHERE
                 "ProductName" = {originalProductName}
                 AND "AccountEmail" = {asset.AccountEmail.Value}
             """
        );
    }

    /// <inheritdoc />
    public async Task UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string originalProductName, string newProductName)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "Asset"
             SET "ProductName" = {newProductName}
             WHERE "ProductName" = {originalProductName} AND "AccountEmail" = {accountEmail}
             """
        );
    }

    private static Asset ToDomain(OrmAsset e) => Asset.Reconstitute(
        e.ProductName ?? string.Empty, e.AccountEmail ?? string.Empty, e.Item ?? string.Empty,
        e.Amount, e.MonetaryUnit ?? string.Empty, e.Note, e.Deleted, e.Created, e.Updated);

    private static OrmAsset ToEntity(Asset a) => new()
    {
        ProductName = a.ProductName,
        AccountEmail = a.AccountEmail.Value,
        Item = a.Item,
        Amount = a.Balance.Amount,
        MonetaryUnit = a.Balance.Currency,
        Note = a.Note,
        Deleted = a.Deleted,
        Created = a.Created,
        Updated = a.Updated
    };
}
