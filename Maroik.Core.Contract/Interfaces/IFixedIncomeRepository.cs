using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the FixedIncome table (recurring monthly income).
/// </summary>
public interface IFixedIncomeRepository
{
    /// <summary>Returns all fixed income records for the given account.</summary>
    public Task<List<FixedIncome>> GetFixedIncomesAsync(string email);

    /// <summary>Inserts a new fixed income row.</summary>
    public Task CreateFixedIncomeAsync(FixedIncome fixedIncome);

    /// <summary>Updates an existing fixed income row.</summary>
    public Task UpdateFixedIncomeAsync(FixedIncome fixedIncome);

    /// <summary>Deletes a fixed income row (hard delete).</summary>
    public Task DeleteFixedIncomeAsync(FixedIncome fixedIncome);
}
