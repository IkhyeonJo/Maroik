using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the FixedExpenditure table (recurring monthly charges).
/// </summary>
public interface IFixedExpenditureRepository
{
    /// <summary>Returns all fixed expenditure records for the given account.</summary>
    public Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string email);

    /// <summary>Inserts a new fixed expenditure row.</summary>
    public Task CreateFixedExpenditureAsync(FixedExpenditureRequest request);

    /// <summary>Updates an existing fixed expenditure row.</summary>
    public Task UpdateFixedExpenditureAsync(FixedExpenditureRequest request);

    /// <summary>Deletes a fixed expenditure row (hard delete).</summary>
    public Task DeleteFixedExpenditureAsync(FixedExpenditureRequest request);

    /// <summary>
    /// Renames the MyDepositAsset field on all fixed expenditures belonging to <paramref name="accountEmail"/>
    /// when the linked asset is renamed. Returns the number of rows updated.
    /// </summary>
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}
