using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;

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
    public async Task<List<Account>> GetAllAsync()
        => (await _context.Accounts.ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<List<Account>> GetAccountsByNicknamesAsync(IEnumerable<string> nicknames)
    {
        var list = nicknames.ToList();
        return (await _context.Accounts
            .Where(a => list.Contains(a.Nickname ?? ""))
            .ToListAsync())
            .Select(ToDomain).ToList();
    }

    /// <inheritdoc />
    public async Task<Account?> GetAccountByEmailAsync(string email)
    {
        OrmAccount? entity = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == email);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Account?> GetAccountByRegistrationTokenAsync(string rawToken)
    {
        OrmAccount? entity = await _context.Accounts.FirstOrDefaultAsync(x => x.RegistrationToken == rawToken);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<Account?> GetAccountByResetPasswordTokenAsync(string rawToken)
    {
        OrmAccount? entity = await _context.Accounts.FirstOrDefaultAsync(x => x.ResetPasswordToken == rawToken);
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task CreateAccountAsync(Account account)
    {
        _ = await _context.Accounts.AddAsync(ToEntity(account));
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateAccountAsync(Account account)
    {
        _ = _context.Accounts.Update(ToEntity(account));
        _ = await _context.SaveChangesAsync();
    }

    private static Account ToDomain(OrmAccount e) => Account.Reconstitute(
        e.Email ?? string.Empty, e.HashedPassword ?? string.Empty, e.Nickname ?? string.Empty,
        e.AvatarImagePath, e.Role ?? string.Empty, e.TimeZoneIanaId ?? "UTC",
        e.DefaultMonetaryUnit, e.Locked, e.LoginAttempt, e.EmailConfirmed,
        e.AgreedServiceTerms, e.RegistrationToken, e.ResetPasswordToken,
        e.Created, e.Updated, e.Message, e.Deleted);

    private static OrmAccount ToEntity(Account a) => new()
    {
        Email = a.Email.Value,
        HashedPassword = a.HashedPassword,
        Nickname = a.Nickname,
        AvatarImagePath = a.AvatarImagePath,
        Role = a.Role,
        TimeZoneIanaId = a.TimeZone.Value,
        DefaultMonetaryUnit = a.DefaultMonetaryUnit,
        Locked = a.Locked,
        LoginAttempt = a.LoginAttempt,
        EmailConfirmed = a.EmailConfirmed,
        AgreedServiceTerms = a.AgreedServiceTerms,
        RegistrationToken = a.RegistrationToken,
        ResetPasswordToken = a.ResetPasswordToken,
        Created = a.Created,
        Updated = a.Updated,
        Message = a.Message,
        Deleted = a.Deleted
    };
}
