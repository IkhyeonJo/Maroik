using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IFixedExpenditureRepository
{
    public Task<List<FixedExpenditureResponse>> GetFixedExpendituresAsync(string email);
    public Task CreateFixedExpenditureAsync(FixedExpenditureRequest request);
    public Task UpdateFixedExpenditureAsync(FixedExpenditureRequest request);
    public Task DeleteFixedExpenditureAsync(FixedExpenditureRequest request);
    public Task<int> UpdateMyDepositAssetWithProductNameAsync(string accountEmail, string oldMyDepositAsset, string newMyDepositAsset);
}
