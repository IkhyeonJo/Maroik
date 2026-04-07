using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for fixed (recurring) expenditure business logic.
/// </summary>
public interface IFixedExpenditureService
{
    /// <summary>Returns all fixed expenditure records for the given account.</summary>
    Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string accountEmail);

    /// <summary>Validates and creates a new fixed expenditure record.</summary>
    Task<ServiceResult> CreateAsync(string accountEmail, FixedExpenditureRequest request);

    /// <summary>Validates and updates an existing fixed expenditure record.</summary>
    Task<ServiceResult> UpdateAsync(string accountEmail, FixedExpenditureRequest request);

    /// <summary>Deletes a fixed expenditure record, verifying it belongs to the given account.</summary>
    Task<ServiceResult> DeleteAsync(string accountEmail, long id);
}
