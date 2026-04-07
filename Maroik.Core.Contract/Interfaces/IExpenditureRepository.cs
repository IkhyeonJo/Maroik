using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Expenditure table.
/// </summary>
public interface IExpenditureRepository
{
    /// <summary>Returns all expenditure records for the given account (no date filter).</summary>
    public Task<List<ExpenditureResponse>> GetExpendituresAsync(string email);

    /// <summary>Returns expenditure records for the given account filtered to the specified calendar year, converted to the user's time zone.</summary>
    public Task<List<ExpenditureResponse>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId);

    /// <summary>Returns expenditure records for the given account filtered to the specified year-month (e.g. "2024-03"), in the user's time zone.</summary>
    public Task<List<ExpenditureResponse>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId);

    /// <summary>Inserts a new expenditure row.</summary>
    public Task CreateExpenditureAsync(ExpenditureRequest request);

    /// <summary>Updates an existing expenditure row.</summary>
    public Task UpdateExpenditureAsync(ExpenditureRequest request);

    /// <summary>Deletes an expenditure row (hard delete).</summary>
    public Task DeleteExpenditureAsync(ExpenditureRequest request);

    /// <summary>
    /// Renames the MyDepositAsset field on all expenditures belonging to <paramref name="accountEmail"/>
    /// from <paramref name="oldMyDepositAsset"/> to <paramref name="newMyDepositAsset"/>.
    /// Called when an asset is renamed. Returns the number of rows updated.
    /// </summary>
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}
