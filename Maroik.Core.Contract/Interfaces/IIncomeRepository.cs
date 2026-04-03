using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Contract.Interfaces;

public interface IIncomeRepository
{
    /// <summary>
    /// Gets all incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<Income>> GetIncomesAsync(string email);
    
    /// <summary>
    /// Gets current year incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="year"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public Task<List<Income>> GetCurrentYearIncomesAsync(string email, string year, string timeZoneIanaId);

    /// <summary>
    /// Gets current year-month incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="yearMonth"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public Task<List<Income>> GetCurrentYearMonthIncomesAsync(string email, string yearMonth, string timeZoneIanaId);

    /// <summary>
    /// Creates an income.
    /// </summary>
    /// <param name="income"></param>
    /// <returns></returns>
    public Task CreateIncomeAsync(Income income);

    /// <summary>
    /// Updates an income.
    /// </summary>
    /// <param name="income"></param>
    /// <returns></returns>
    public Task UpdateIncomeAsync(Income income);

    /// <summary>
    /// Removes an income.
    /// </summary>
    /// <param name="income"></param>
    /// <returns></returns>
    public Task DeleteIncomeAsync(Income income);
}