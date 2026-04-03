using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class FixedExpenditureRepository : IFixedExpenditureRepository
{
    private readonly ApplicationDbContext _context;

    public FixedExpenditureRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all fixed expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<FixedExpenditure>> GetFixedExpendituresAsync(string email)
    {
        return await _context.FixedExpenditures.Where(x => x.AccountEmail == email).ToListAsync();
    }

    /// <summary>
    /// Creates a fixed expenditure.
    /// </summary>
    /// <param name="fixedExpenditure"></param>
    /// <returns></returns>
    public async Task CreateFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        _ = await _context.FixedExpenditures.AddAsync(fixedExpenditure);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a fixed expenditure.
    /// </summary>
    /// <param name="fixedExpenditure"></param>
    /// <returns></returns>
    public async Task UpdateFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        _ = _context.FixedExpenditures.Update(fixedExpenditure);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes a fixed expenditure.
    /// </summary>
    /// <param name="fixedExpenditure"></param>
    /// <returns></returns>
    public async Task DeleteFixedExpenditureAsync(FixedExpenditure fixedExpenditure)
    {
        _ = _context.FixedExpenditures.Remove(fixedExpenditure);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates my deposit asset name.
    /// </summary>
    /// <param name="accountEmail"></param>
    /// <param name="oldMyDepositAsset"></param>
    /// <param name="newMyDepositAsset"></param>
    /// <returns></returns>
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