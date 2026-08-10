using Maroik.Core.Domain.Menu;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="Category"/> (navigation menu) persistence.
/// </summary>
public interface ICategoryRepository : IGenericRepository<Category>
{
    /// <summary>Returns all top-level navigation categories.</summary>
    Task<List<Category>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes the category with the given ID.</summary>
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
