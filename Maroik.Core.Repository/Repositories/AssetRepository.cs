using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _context;

    public AssetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AssetResponse>> GetAssetsAsync(string email)
    {
        return (await _context.Assets.Where(x => x.AccountEmail == email).ToListAsync()).Adapt<List<AssetResponse>>();
    }

    public async Task<AssetResponse?> GetAssetAsync(string email, string productName)
    {
        Asset? asset = await _context.Assets.FirstOrDefaultAsync(a => a.AccountEmail == email && a.ProductName == productName);
        return asset?.Adapt<AssetResponse>();
    }

    public async Task CreateAssetAsync(AssetRequest request)
    {
        _ = await _context.Assets.AddAsync(request.Adapt<Asset>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task UpdateAssetAsync(AssetRequest request)
    {
        _ = _context.Assets.Update(request.Adapt<Asset>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task<int> UpdateAssetWithProductNameAsync(AssetRequest request, string originalProductName)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "Asset"
             SET
                 "ProductName" = {request.ProductName},
                 "Item" = {request.Item},
                 "Amount" = {request.Amount},
                 "MonetaryUnit" = {request.MonetaryUnit},
                 "Note" = {request.Note},
                 "Deleted" = {request.Deleted},
                 "Updated" = {request.Updated}
             WHERE
                 "ProductName" = {originalProductName}
                 AND "AccountEmail" = {request.AccountEmail}
             """
        );
    }
}
