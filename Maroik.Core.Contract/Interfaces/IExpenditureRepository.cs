using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Expenditure table.
/// </summary>
public interface IExpenditureRepository
{
    /// <summary>Returns all expenditure records for the given account (no date filter).</summary>
    public Task<List<Expenditure>> GetExpendituresAsync(string email);

    /// <summary>Returns expenditure records for the given account filtered to the specified calendar year, converted to the user's time zone.</summary>
    public Task<List<Expenditure>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId);

    /// <summary>Returns expenditure records for the given account filtered to the specified year-month (e.g. "2024-03"), in the user's time zone.</summary>
    public Task<List<Expenditure>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId);

    /// <summary>Inserts a new expenditure row.</summary>
    public Task CreateExpenditureAsync(Expenditure expenditure);

    /// <summary>Updates an existing expenditure row.</summary>
    public Task UpdateExpenditureAsync(Expenditure expenditure);

    /// <summary>Deletes an expenditure row (hard delete).</summary>
    public Task DeleteExpenditureAsync(Expenditure expenditure);

    /// <summary>
    /// Renames the MyDepositAsset field on all expenditures belonging to <paramref name="accountEmail"/>
    /// from <paramref name="oldMyDepositAsset"/> to <paramref name="newMyDepositAsset"/>.
    /// Returns the number of rows updated.
    /// </summary>
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}
