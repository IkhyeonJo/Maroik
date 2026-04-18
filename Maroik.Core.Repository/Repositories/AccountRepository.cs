using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IAccountRepository"/>.</summary>
public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="AccountRepository"/> with <paramref name="context"/>.</summary>
    public AccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<AccountResponse>> GetAllAsync()
    {
        return (await _context.Accounts.ToListAsync()).Adapt<List<AccountResponse>>();
    }

    /// <inheritdoc />
    public async Task<List<AccountResponse>> GetAccountsByNicknamesAsync(IEnumerable<string> nicknames)
    {
        var list = nicknames.ToList();
        return (await _context.Accounts
            .Where(a => list.Contains(a.Nickname ?? ""))
            .ToListAsync())
            .Adapt<List<AccountResponse>>();
    }

    /// <inheritdoc />
    public async Task<AccountResponse?> GetAccountByEmailAsync(string email)
    {
        Account? account = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == email);
        return account?.Adapt<AccountResponse>();
    }

    /// <inheritdoc />
    public async Task<AccountResponse?> GetAccountByRegistrationTokenAsync(string rawToken)
    {
        Account? account = await _context.Accounts.FirstOrDefaultAsync(x => x.RegistrationToken == rawToken);
        return account?.Adapt<AccountResponse>();
    }

    /// <inheritdoc />
    public async Task<AccountResponse?> GetAccountByResetPasswordTokenAsync(string rawToken)
    {
        Account? account = await _context.Accounts.FirstOrDefaultAsync(x => x.ResetPasswordToken == rawToken);
        return account?.Adapt<AccountResponse>();
    }

    /// <inheritdoc />
    public async Task CreateAccountAsync(AccountRequest request)
    {
        _ = await _context.Accounts.AddAsync(request.Adapt<Account>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAccountAsync(AccountRequest request)
    {
        _ = _context.Accounts.Update(request.Adapt<Account>());
        _ = await _context.SaveChangesAsync();
    }
}
