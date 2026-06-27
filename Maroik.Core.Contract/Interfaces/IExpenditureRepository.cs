using Maroik.Core.Contract.Domains.Finance;
namespace Maroik.Core.Contract.Interfaces;

public interface IExpenditureRepository : IGenericRepository<Expenditure>
{
    Task<List<Expenditure>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);
    Task<Expenditure?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default);
    Task<List<Expenditure>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Expenditure?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default);
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
    Task UpdateMyDepositAssetNameAsync(string accountEmail, string oldName, string newName, CancellationToken ct = default);
}
