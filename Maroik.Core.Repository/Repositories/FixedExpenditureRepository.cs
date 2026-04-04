using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class FixedExpenditureRepository : IFixedExpenditureRepository
{
    private readonly ApplicationDbContext _context;

    public FixedExpenditureRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string email)
    {
        return (await _context.FixedExpenditures.Where(x => x.AccountEmail == email).ToListAsync()).Adapt<List<FixedExpenditureResponse>>();
    }

    public async Task CreateFixedExpenditureAsync(FixedExpenditureRequest request)
    {
        _ = await _context.FixedExpenditures.AddAsync(request.Adapt<FixedExpenditure>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task UpdateFixedExpenditureAsync(FixedExpenditureRequest request)
    {
        _ = _context.FixedExpenditures.Update(request.Adapt<FixedExpenditure>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task DeleteFixedExpenditureAsync(FixedExpenditureRequest request)
    {
        _ = _context.FixedExpenditures.Remove(request.Adapt<FixedExpenditure>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""
             UPDATE "FixedExpenditure"
             SET "MyDepositAsset" = {newMyDepositAsset}
             WHERE "MyDepositAsset" = {oldMyDepositAsset} AND "AccountEmail" = {accountEmail}
             """
        );
    }
}
