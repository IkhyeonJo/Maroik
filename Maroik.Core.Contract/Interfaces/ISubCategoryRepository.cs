using Maroik.Core.Contract.Domains.Menu;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the SubCategory (sidebar dropdown menu item) table.
/// Lookup/create/delete operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface ISubCategoryRepository : IGenericRepository<SubCategory>
{
}
