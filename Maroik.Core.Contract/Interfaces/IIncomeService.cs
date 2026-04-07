using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for income business logic. Enforces ownership checks before delegating to the repository.
/// </summary>
public interface IIncomeService
{
    /// <summary>Returns all income records for the given account.</summary>
    Task<List<IncomeResponse>> GetIncomesAsync(string accountEmail);

    /// <summary>Validates and creates a new income record owned by the given account.</summary>
    Task<ServiceResult> CreateAsync(string accountEmail, IncomeRequest request);

    /// <summary>Validates and updates an income record, verifying it belongs to the given account.</summary>
    Task<ServiceResult> UpdateAsync(string accountEmail, IncomeRequest request);

    /// <summary>Deletes an income record, verifying it belongs to the given account.</summary>
    Task<ServiceResult> DeleteAsync(string accountEmail, long id);
}
