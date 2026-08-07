using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;

    public AccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Gets all registered accounts.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Account>> GetAllAsync()
    {
        return await _context.Accounts.ToListAsync();
    }
    /// <summary>
    /// Gets an account by email.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<Account> GetAccountByEmailAsync(string email)
    {
        return await _context.Accounts.FirstOrDefaultAsync(x => x.Email == email);
    }
    /// <summary>
    /// Creates an account.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public async Task CreateAccountAsync(Account account)
    {
        _ = await _context.Accounts.AddAsync(account);
        _ = await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Updates an account.
    /// </summary>
    /// <param name="account"></param>
    /// <returns></returns>
    public async Task UpdateAccountAsync(Account account)
    {
        _ = _context.Accounts.Update(account);
        _ = await _context.SaveChangesAsync();
    }
    /// <summary>
    /// Atomically increments the failed-login counter and locks the account once
    /// maxLoginAttempt is reached. A single UPDATE statement makes the read-increment-write
    /// atomic at the database level, so concurrent login attempts against the same account
    /// cannot race and lose increments the way a GetAccountByEmailAsync + UpdateAccountAsync
    /// pair would.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="maxLoginAttempt"></param>
    /// <param name="lockedMessage"></param>
    /// <returns></returns>
    public async Task<Account> IncrementLoginAttemptAsync(string email, long maxLoginAttempt, string lockedMessage)
    {
        return await _context.Accounts.FromSqlInterpolated(
            $"""
             WITH updated AS (
                 UPDATE "Account"
                 SET
                     "LoginAttempt" = "LoginAttempt" + 1,
                     "Locked" = CASE WHEN "LoginAttempt" + 1 >= {maxLoginAttempt} THEN true ELSE "Locked" END,
                     "Message" = CASE WHEN "LoginAttempt" + 1 >= {maxLoginAttempt} THEN {lockedMessage} ELSE "Message" END
                 WHERE "Email" = {email}
                 RETURNING *
             )
             SELECT * FROM updated
             """
        ).FirstOrDefaultAsync();
    }
}