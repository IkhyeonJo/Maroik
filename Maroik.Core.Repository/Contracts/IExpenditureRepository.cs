using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface IExpenditureRepository
{
    /// <summary>
    /// Gets all expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<Expenditure>> GetExpendituresAsync(string email);

    /// <summary>
    /// Gets current year expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="year"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public Task<List<Expenditure>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId);
    
    /// <summary>
    /// Gets current year-month expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="yearMonth"></param>
    /// <param name="timeZoneIanaId"></param>
    /// <returns></returns>
    public Task<List<Expenditure>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId);

    /// <summary>
    /// Creates an expenditure.
    /// </summary>
    /// <param name="expenditure"></param>
    /// <returns></returns>
    public Task CreateExpenditureAsync(Expenditure expenditure);

    /// <summary>
    /// Updates an expenditure.
    /// </summary>
    /// <param name="expenditure"></param>
    /// <returns></returns>
    public Task UpdateExpenditureAsync(Expenditure expenditure);

    /// <summary>
    /// Removes an expenditure.
    /// </summary>
    /// <param name="expenditure"></param>
    /// <returns></returns>
    public Task DeleteExpenditureAsync(Expenditure expenditure);

    /// <summary>
    /// Updates my deposit asset name.
    /// </summary>
    /// <param name="accountEmail"></param>
    /// <param name="oldMyDepositAsset"></param>
    /// <param name="newMyDepositAsset"></param>
    /// <returns></returns>
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}