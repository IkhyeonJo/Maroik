using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IIncomeService
{
    Task<ServiceResult> CreateAsync(string accountEmail, IncomeRequest request);
    Task<ServiceResult> UpdateAsync(string accountEmail, IncomeRequest request);
    Task<ServiceResult> DeleteAsync(string accountEmail, long id);
}
