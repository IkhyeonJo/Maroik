using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface IAccountRepository
{
    /// <summary>
    /// Gets all registered accounts.
    /// </summary>
    /// <returns></returns>
    public Task<List<Account>> GetAllAsync();
    /// <summary>
    /// Gets an account by email.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<Account> GetAccountByEmailAsync(string email);
    /// <summary>
    /// Creates an account.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public Task CreateAccountAsync(Account account);
    /// <summary>
    /// Updates an account.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public Task UpdateAccountAsync(Account account);
    /// <summary>
    /// Gets an account by email with a database row lock (SELECT ... FOR UPDATE).
    /// Must be called inside an active transaction; the lock is held until commit/rollback.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<Account> GetAccountForUpdateAsync(string email);
}