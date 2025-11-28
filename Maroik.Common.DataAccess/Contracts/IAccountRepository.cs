using Maroik.Common.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts;

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
}