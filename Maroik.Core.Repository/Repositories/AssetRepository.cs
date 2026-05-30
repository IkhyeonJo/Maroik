using Maroik.Core.Contract.Domains.Finance;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmAsset = Maroik.Core.PostgreSQL.Models.Asset;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Asset"/> domain objects.
/// Includes a raw SQL update method for cases where the composite primary key (ProductName, AccountEmail) changes.
/// </summary>
public class AssetRepository(ApplicationDbContext context)
    : GenericRepository<Asset, OrmAsset>(context), IAssetRepository
{
    protected override DbSet<OrmAsset> Set => Context.Assets;

    /// <inheritdoc />
    public async Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName)
    {
        // Raw SQL is required because the composite PK (ProductName, AccountEmail) changes.
        return await Context.Database.ExecuteSqlInterpolatedAsync(
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
             """);
    }

    protected override Asset ToDomain(OrmAsset e) => Asset.Reconstitute(
        e.ProductName ?? string.Empty, e.AccountEmail ?? string.Empty, e.Item ?? string.Empty,
        e.Amount, e.MonetaryUnit ?? string.Empty, e.Note, e.Deleted, e.Created, e.Updated);

    protected override OrmAsset ToEntity(Asset a) => new()
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
