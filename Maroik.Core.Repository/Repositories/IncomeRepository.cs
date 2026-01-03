using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class IncomeRepository(ApplicationDbContext context) : IIncomeRepository
{
    /// <summary>
    /// Gets all incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<Income>> GetIncomesAsync(string email)
    {
        return await context.Incomes.Where(x => x.AccountEmail == email).ToListAsync();
    }
    
    /// <summary>
    /// Gets current year incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="year"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public async Task<List<Income>> GetCurrentYearIncomesAsync(string email, string year, string timeZoneIanaId)
    {
        return await context.Incomes.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Income"
             WHERE 
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY') = {year}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync();
    }

    /// <summary>
    /// Gets current year-month incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="yearMonth"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public async Task<List<Income>> GetCurrentYearMonthIncomesAsync(string email, string yearMonth, string timeZoneIanaId)
    {
        return await context.Incomes.FromSqlInterpolated
        (
            $"""
             SELECT *
             FROM "Income"
             WHERE 
                 TO_CHAR("Created" AT TIME ZONE 'UTC' AT TIME ZONE {timeZoneIanaId}, 'YYYY-MM') = {yearMonth}
                 AND "AccountEmail" = {email}
             """
        ).ToListAsync();
    }

    /// <summary>
    /// Creates an income.
    /// </summary>
    /// <param name="income"></param>
    /// <returns></returns>
    public async Task CreateIncomeAsync(Income income)
    {
        _ = await context.Incomes.AddAsync(income);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an income.
    /// </summary>
    /// <param name="income"></param>
    /// <returns></returns>
    public async Task UpdateIncomeAsync(Income income)
    {
        _ = context.Incomes.Update(income);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Removes an income.
    /// </summary>
    /// <param name="income"></param>
    /// <returns></returns>
    public async Task DeleteIncomeAsync(Income income)
    {
        _ = context.Incomes.Remove(income);
        _ = await context.SaveChangesAsync();
    }
}