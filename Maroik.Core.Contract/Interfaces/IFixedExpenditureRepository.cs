using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the FixedExpenditure table (recurring monthly charges).
/// </summary>
public interface IFixedExpenditureRepository
{
    /// <summary>Returns all fixed expenditure records for the given account.</summary>
    public Task<List<FixedExpenditure>> GetFixedExpendituresAsync(string email);

    /// <summary>Inserts a new fixed expenditure row.</summary>
    public Task CreateFixedExpenditureAsync(FixedExpenditure fixedExpenditure);

    /// <summary>Updates an existing fixed expenditure row.</summary>
    public Task UpdateFixedExpenditureAsync(FixedExpenditure fixedExpenditure);

    /// <summary>Deletes a fixed expenditure row (hard delete).</summary>
    public Task DeleteFixedExpenditureAsync(FixedExpenditure fixedExpenditure);

    /// <summary>
    /// Renames the MyDepositAsset field on all fixed expenditures belonging to <paramref name="accountEmail"/>
    /// when the linked asset is renamed. Returns the number of rows updated.
    /// </summary>
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}
