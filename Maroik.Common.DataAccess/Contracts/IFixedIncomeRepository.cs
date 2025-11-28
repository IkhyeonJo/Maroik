using Maroik.Common.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts;

public interface IFixedIncomeRepository
{
    /// <summary>
    /// Gets all fixed incomes for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<FixedIncome>> GetFixedIncomesAsync(string email);

    /// <summary>
    /// Creates a fixed income.
    /// </summary>
    /// <param name="fixedIncome"></param>
    /// <returns></returns>
    public Task CreateFixedIncomeAsync(FixedIncome fixedIncome);

    /// <summary>
    /// Updates a fixed income.
    /// </summary>
    /// <param name="fixedIncome"></param>
    /// <returns></returns>
    public Task UpdateFixedIncomeAsync(FixedIncome fixedIncome);

    /// <summary>
    /// Removes a fixed income.
    /// </summary>
    /// <param name="fixedIncome"></param>
    /// <returns></returns>
    public Task DeleteFixedIncomeAsync(FixedIncome fixedIncome);
}