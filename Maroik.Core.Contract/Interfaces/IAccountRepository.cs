using Maroik.Core.Contract.Domains.Account;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for direct Account table operations.
/// All CRUD operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface IAccountRepository : IGenericRepository<Account>
{
}
