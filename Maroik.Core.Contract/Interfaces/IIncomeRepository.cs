using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Income table.
/// All CRUD operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IIncomeRepository : IGenericRepository<Income>
{
}
