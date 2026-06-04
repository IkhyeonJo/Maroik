using Maroik.Core.Contract.Domains.Finance;
namespace Maroik.Core.Contract.Interfaces;
public interface IFixedIncomeRepository : IGenericRepository<FixedIncome>
{
    Task<List<FixedIncome>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);
    Task<FixedIncome?> FindByEmailAndIdAsync(string accountEmail, long id, CancellationToken ct = default);
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
