using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the FixedExpenditure table (recurring monthly charges).
/// Lookup/create/delete operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IFixedExpenditureRepository : IGenericRepository<FixedExpenditure>
{
}
