using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface IFixedIncomeRepository
{
    public Task<List<FixedIncomeResponse>> GetFixedIncomesAsync(string email);
    public Task CreateFixedIncomeAsync(FixedIncomeRequest request);
    public Task UpdateFixedIncomeAsync(FixedIncomeRequest request);
    public Task DeleteFixedIncomeAsync(FixedIncomeRequest request);
}
