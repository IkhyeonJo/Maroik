using Maroik.Core.Contract.Domains.Finance;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the FixedIncome table (recurring monthly income).
/// Lookup/create/delete operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IFixedIncomeRepository : IGenericRepository<FixedIncome>
{
}
