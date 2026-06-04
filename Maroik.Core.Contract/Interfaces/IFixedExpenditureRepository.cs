using Maroik.Core.Contract.Domains.Finance;
namespace Maroik.Core.Contract.Interfaces;
public interface IFixedExpenditureRepository : IGenericRepository<FixedExpenditure>
{
    Task<List<FixedExpenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);
    Task<FixedExpenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default);
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
    Task UpdateMyDepositAssetNameAsync(string accountEmail, string oldName, string newName, CancellationToken ct = default);
}
