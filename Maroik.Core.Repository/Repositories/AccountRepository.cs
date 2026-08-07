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
    /// Gets an account by email with a database row lock (SELECT ... FOR UPDATE).
    /// Must be called inside an active transaction; the lock is held until commit/rollback,
    /// so concurrent requests against the same account (e.g. parallel login attempts) queue
    /// up and each sees the latest committed state instead of racing on a stale read.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<Account> GetAccountForUpdateAsync(string email)
    {
        return await _context.Accounts.FromSqlInterpolated(
            $"""
             SELECT *
             FROM "Account"
             WHERE "Email" = {email}
             FOR UPDATE
             """).FirstOrDefaultAsync();
    }
}