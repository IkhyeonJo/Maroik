using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IFixedIncomeService
{
    Task<List<FixedIncomeResponse>> GetFixedIncomesAsync(string accountEmail);
    Task<ServiceResult> CreateAsync(string accountEmail, FixedIncomeRequest request);
    Task<ServiceResult> UpdateAsync(string accountEmail, FixedIncomeRequest request);
    Task<ServiceResult> DeleteAsync(string accountEmail, long id);
}
