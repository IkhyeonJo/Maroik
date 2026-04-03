using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Contract.Interfaces;

public interface IFixedExpenditureRepository
{
    /// <summary>
    /// Gets all fixed expenditures for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<FixedExpenditure>> GetFixedExpendituresAsync(string email);

    /// <summary>
    /// Creates a fixed expenditure.
    /// </summary>
    /// <param name="fixedExpenditure"></param>
    /// <returns></returns>
    public Task CreateFixedExpenditureAsync(FixedExpenditure fixedExpenditure);

    /// <summary>
    /// Updates a fixed expenditure.
    /// </summary>
    /// <param name="fixedExpenditure"></param>
    /// <returns></returns>
    public Task UpdateFixedExpenditureAsync(FixedExpenditure fixedExpenditure);

    /// <summary>
    /// Removes a fixed expenditure.
    /// </summary>
    /// <param name="fixedExpenditure"></param>
    /// <returns></returns>
    public Task DeleteFixedExpenditureAsync(FixedExpenditure fixedExpenditure);

    /// <summary>
    /// Updates my deposit asset name.
    /// </summary>
    /// <param name="accountEmail"></param>
    /// <param name="oldMyDepositAsset"></param>
    /// <param name="newMyDepositAsset"></param>
    /// <returns></returns>
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}