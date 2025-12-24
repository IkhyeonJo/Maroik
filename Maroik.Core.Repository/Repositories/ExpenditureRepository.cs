using Maroik.Core.Repository.Contracts;
using Maroik.Core.Repository.Data;
using Maroik.Core.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class ExpenditureRepository : IExpenditureRepository
{
    private readonly ApplicationDbContext _context;

    public ExpenditureRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<Expenditure>> GetExpendituresAsync(string email)
    {
        return await _context.Expenditures.Where(x => x.AccountEmail == email).ToListAsync();
    }

    /// <summary>
    /// Gets current year expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="year"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public async Task<List<Expenditure>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId)
    {
        return await _context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE 
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY') = {year}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync();
    }

    /// <summary>
    /// Gets current year-month expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="yearMonth"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public async Task<List<Expenditure>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId)
    {
        return await _context.Expenditures.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Expenditure"
             WHERE 
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY-MM') = {yearMonth}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync();
    }

    /// <summary>
    /// Creates an expenditure.
    /// </summary>
    /// <param name="expenditure"></param>
    /// <returns></returns>
    public async Task CreateExpenditureAsync(Expenditure expenditure)
    {
        _ = await _context.Expenditures.AddAsync(expenditure);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an expenditure.
    /// </summary>
    /// <param name="expenditure"></param>
    /// <returns></returns>
    public async Task UpdateExpenditureAsync(Expenditure expenditure)
    {
        _ = _context.Expenditures.Update(expenditure);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes an expenditure.
    /// </summary>
    /// <param name="expenditure"></param>
    /// <returns></returns>
    public async Task DeleteExpenditureAsync(Expenditure expenditure)
    {
        _ = _context.Expenditures.Remove(expenditure);
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
              UPDATE "Expenditure"
              SET "MyDepositAsset" = {newMyDepositAsset}
              WHERE "MyDepositAsset" = {oldMyDepositAsset} AND "AccountEmail" = {accountEmail}
              """
        );
    }
}