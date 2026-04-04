using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IExpenditureRepository
{
    public Task<List<ExpenditureResponse>> GetExpendituresAsync(string email);
    public Task<List<ExpenditureResponse>> GetCurrentYearExpendituresAsync(string email, string year, string timeZoneIanaId);
    public Task<List<ExpenditureResponse>> GetCurrentYearMonthExpendituresAsync(string email, string yearMonth, string timeZoneIanaId);
    public Task CreateExpenditureAsync(ExpenditureRequest request);
    public Task UpdateExpenditureAsync(ExpenditureRequest request);
    public Task DeleteExpenditureAsync(ExpenditureRequest request);
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}
