using Maroik.Core.Contract.Domains.Menu;
namespace Maroik.Core.Contract.Interfaces;
public interface ICategoryRepository : IGenericRepository<Category>
{
    Task<List<Category>> GetAllAsync(CancellationToken ct = default);
    Task DeleteByIdAsync(int id, CancellationToken ct = default);
}
