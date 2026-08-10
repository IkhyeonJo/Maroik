using Maroik.Core.Domain.Finance;
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
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmAsset"/> rows.</summary>
    protected override DbSet<OrmAsset> Set => Context.Assets;

    /// <inheritdoc />
    public async Task<int> UpdateAssetWithProductNameAsync(Asset asset, string originalProductName, CancellationToken ct = default)
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
             """, ct);
    }

    /// <summary>Maps a persisted <see cref="OrmAsset"/> row to the <see cref="Asset"/> domain object.</summary>
    protected override Asset ToDomain(OrmAsset e) => Asset.Reconstitute(
        e.ProductName ?? "", e.AccountEmail ?? "", e.Item ?? "",
        e.Amount, e.MonetaryUnit ?? "", e.Note, e.Deleted, e.Created, e.Updated);

    /// <summary>Maps an <see cref="Asset"/> domain object to its <see cref="OrmAsset"/> persistence representation.</summary>
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

    /// <inheritdoc />
    public Task<List<Asset>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == accountEmail, ct: ct);

    /// <inheritdoc />
    public Task<Asset?> FindByEmailAndProductNameAsync(string accountEmail, string productName, CancellationToken ct = default)
        => QueryFirstAsync(e => e.AccountEmail == accountEmail && e.ProductName == productName, ct);

    /// <inheritdoc />
    public async Task<Asset?> FindByEmailAndProductNameForUpdateAsync(string accountEmail, string productName, CancellationToken ct = default)
    {
        // Raw SQL is required because EF Core LINQ cannot emit FOR UPDATE.
        // ToListAsync on the raw root executes the SQL verbatim (no composition),
        // keeping FOR UPDATE at the top level of the statement.
        var entities = await Set.FromSqlInterpolated(
            $"""
             SELECT *
             FROM "Asset"
             WHERE
                 "AccountEmail" = {accountEmail}
                 AND "ProductName" = {productName}
             LIMIT 1
             FOR UPDATE
             """).ToListAsync(ct);

        var entity = entities.FirstOrDefault();
        return entity == null ? null : ToDomain(entity);
    }
}
