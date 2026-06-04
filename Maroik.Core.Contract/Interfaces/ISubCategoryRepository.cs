using Maroik.Core.Contract.Domains.Menu;
namespace Maroik.Core.Contract.Interfaces;
public interface ISubCategoryRepository : IGenericRepository<SubCategory>
{
    Task<List<SubCategory>> GetAllAsync(CancellationToken ct = default);
    Task DeleteByIdAsync(int id, CancellationToken ct = default);
}
