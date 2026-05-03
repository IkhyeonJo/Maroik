using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Income table.
/// </summary>
public interface IIncomeRepository
{
    /// <summary>Returns all income records for the given account (no date filter).</summary>
    public Task<List<Income>> GetIncomesAsync(string email);

    /// <summary>Returns income records for the given account filtered to the specified calendar year, in the user's time zone.</summary>
    public Task<List<Income>> GetCurrentYearIncomesAsync(string email, string year, string timeZoneIanaId);

    /// <summary>Returns income records for the given account filtered to the specified year-month (e.g. "2024-03"), in the user's time zone.</summary>
    public Task<List<Income>> GetCurrentYearMonthIncomesAsync(string email, string yearMonth, string timeZoneIanaId);

    /// <summary>Inserts a new income row.</summary>
    public Task CreateIncomeAsync(Income income);

    /// <summary>Updates an existing income row.</summary>
    public Task UpdateIncomeAsync(Income income);

    /// <summary>Deletes an income row (hard delete).</summary>
    public Task DeleteIncomeAsync(Income income);
}
