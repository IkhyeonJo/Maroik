using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the FixedIncome table (recurring monthly income).
/// </summary>
public interface IFixedIncomeRepository
{
    /// <summary>Returns all fixed income records for the given account.</summary>
    public Task<List<FixedIncomeResponse>> GetFixedIncomesAsync(string email);

    /// <summary>Inserts a new fixed income row.</summary>
    public Task CreateFixedIncomeAsync(FixedIncomeRequest request);

    /// <summary>Updates an existing fixed income row.</summary>
    public Task UpdateFixedIncomeAsync(FixedIncomeRequest request);

    /// <summary>Deletes a fixed income row (hard delete).</summary>
    public Task DeleteFixedIncomeAsync(FixedIncomeRequest request);
}
