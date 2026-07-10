using Maroik.Core.Contract.Domains.Menu;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="SubCategory"/> (navigation dropdown item) persistence.
/// </summary>
public interface ISubCategoryRepository : IGenericRepository<SubCategory>
{
    /// <summary>Returns all sub-category (dropdown menu item) rows.</summary>
    Task<List<SubCategory>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Deletes the sub-category with the given ID.</summary>
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
