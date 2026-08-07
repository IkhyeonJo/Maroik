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
    /// Atomically increments the failed-login counter for an account and locks it once
    /// maxLoginAttempt is reached. Runs as a single UPDATE so concurrent login attempts
    /// against the same account cannot race and lose increments.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="maxLoginAttempt"></param>
    /// <param name="lockedMessage"></param>
    /// <returns>The account row after the update, or null if no account matches the email.</returns>
    public Task<Account> IncrementLoginAttemptAsync(string email, long maxLoginAttempt, string lockedMessage);
}