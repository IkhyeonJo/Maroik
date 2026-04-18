using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IFixedExpenditureRepository"/>.</summary>
public class FixedExpenditureRepository : IFixedExpenditureRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="FixedExpenditureRepository"/> with <paramref name="context"/>.</summary>
    public FixedExpenditureRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string email)
    {
        return (await _context.FixedExpenditures.Where(x => x.AccountEmail == email).ToListAsync()).Adapt<List<FixedExpenditureResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateFixedExpenditureAsync(FixedExpenditureRequest request)
    {
        _ = await _context.FixedExpenditures.AddAsync(request.Adapt<FixedExpenditure>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateFixedExpenditureAsync(FixedExpenditureRequest request)
    {
        _ = _context.FixedExpenditures.Update(request.Adapt<FixedExpenditure>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteFixedExpenditureAsync(FixedExpenditureRequest request)
    {
        _ = _context.FixedExpenditures.Remove(request.Adapt<FixedExpenditure>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
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
