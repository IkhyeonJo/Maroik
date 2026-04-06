using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IExpenditureService
{
    Task<List<ExpenditureResponse>> GetExpendituresAsync(string accountEmail);
    Task<ServiceResult> CreateAsync(string accountEmail, ExpenditureRequest request);
    Task<ServiceResult> UpdateAsync(string accountEmail, ExpenditureRequest request);
    Task<ServiceResult> DeleteAsync(string accountEmail, long id);
}
