using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAssetRepository"/> that persists and retrieves
/// <see cref="Asset"/> domain objects via <see cref="ApplicationDbContext"/>.
/// </summary>
public class AssetRepository : IAssetRepository
{
    /// <summary>The EF Core database context used for all data access operations.</summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="AssetRepository"/> with the given
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The EF Core database context injected by the DI container.</param>
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
        var existing = await _context.Assets.FindAsync(asset.ProductName, asset.AccountEmail.Value);
        if (existing == null) return;
        _context.Entry(existing).CurrentValues.SetValues(ToEntity(asset));
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName)
    {
        // Raw SQL is required here because the composite primary key (ProductName, AccountEmail)
        // prevents a standard EF Core Update when ProductName itself changes.
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
        // Rename the product key on the asset row directly via SQL to avoid EF tracking conflicts.
        await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "Asset"
             SET "ProductName" = {newProductName}
             WHERE "ProductName" = {originalProductName} AND "AccountEmail" = {accountEmail}
             """
        );
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmAsset"/> row to the <see cref="Asset"/> domain object,
    /// applying null-coalescing defaults for required string fields.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="Asset"/> domain object.</returns>
    private static Asset ToDomain(OrmAsset e) => Asset.Reconstitute(
        e.ProductName ?? string.Empty, e.AccountEmail ?? string.Empty, e.Item ?? string.Empty,
        e.Amount, e.MonetaryUnit ?? string.Empty, e.Note, e.Deleted, e.Created, e.Updated);

    /// <summary>
    /// Maps an <see cref="Asset"/> domain object to an ORM <see cref="OrmAsset"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="a">The domain object to convert.</param>
    /// <returns>An <see cref="OrmAsset"/> entity suitable for EF Core tracking.</returns>
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
