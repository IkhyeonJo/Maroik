using Maroik.Core.Contract.Domains.Menu;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Category (top-level navigation menu) table.
/// Lookup/create/delete operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface ICategoryRepository : IGenericRepository<Category>
{
}
