using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly ApplicationDbContext _context;

    public AccountRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AccountResponse>> GetAllAsync()
    {
        return (await _context.Accounts.ToListAsync()).Adapt<List<AccountResponse>>();
    }

    public async Task<AccountResponse?> GetAccountByEmailAsync(string email)
    {
        Account? account = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == email);
        return account?.Adapt<AccountResponse>();
    }

    public async Task<AccountResponse?> GetAccountByRegistrationTokenAsync(string rawToken)
    {
        Account? account = await _context.Accounts.FirstOrDefaultAsync(x => x.RegistrationToken == rawToken);
        return account?.Adapt<AccountResponse>();
    }

    public async Task<AccountResponse?> GetAccountByResetPasswordTokenAsync(string rawToken)
    {
        Account? account = await _context.Accounts.FirstOrDefaultAsync(x => x.ResetPasswordToken == rawToken);
        return account?.Adapt<AccountResponse>();
    }

    public async Task CreateAccountAsync(AccountRequest request)
    {
        _ = await _context.Accounts.AddAsync(request.Adapt<Account>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task UpdateAccountAsync(AccountRequest request)
    {
        _ = _context.Accounts.Update(request.Adapt<Account>());
        _ = await _context.SaveChangesAsync();
    }
}
