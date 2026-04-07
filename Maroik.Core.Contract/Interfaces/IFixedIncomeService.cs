using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for fixed (recurring) income business logic.
/// </summary>
public interface IFixedIncomeService
{
    /// <summary>Returns all fixed income records for the given account.</summary>
    Task<List<FixedIncomeResponse>> GetFixedIncomesAsync(string accountEmail);

    /// <summary>Validates and creates a new fixed income record.</summary>
    Task<ServiceResult> CreateAsync(string accountEmail, FixedIncomeRequest request);

    /// <summary>Validates and updates an existing fixed income record.</summary>
    Task<ServiceResult> UpdateAsync(string accountEmail, FixedIncomeRequest request);

    /// <summary>Deletes a fixed income record, verifying it belongs to the given account.</summary>
    Task<ServiceResult> DeleteAsync(string accountEmail, long id);
}
