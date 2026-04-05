using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IFixedExpenditureService
{
    Task<ServiceResult> CreateAsync(string accountEmail, FixedExpenditureRequest request);
    Task<ServiceResult> UpdateAsync(string accountEmail, FixedExpenditureRequest request);
    Task<ServiceResult> DeleteAsync(string accountEmail, long id);
}
