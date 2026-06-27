using Maroik.Core.Contract.Domains.Finance;
namespace Maroik.Core.Contract.Interfaces;

public interface IIncomeRepository : IGenericRepository<Income>
{
    Task<List<Income>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);
    Task<Income?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default);
    Task<List<Income>> GetByAccountEmailAndDateRangeAsync(string accountEmail, DateTime from, DateTime to, CancellationToken ct = default);
    Task<Income?> GetFirstByAccountEmailOrderedByCreatedAsync(string accountEmail, CancellationToken ct = default);
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
