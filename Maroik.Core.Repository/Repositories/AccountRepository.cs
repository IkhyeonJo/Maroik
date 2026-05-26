using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IAccountRepository"/> that persists and retrieves
/// <see cref="Account"/> domain objects via <see cref="ApplicationDbContext"/>.
/// </summary>
public class AccountRepository : IAccountRepository
{
    /// <summary>The EF Core database context used for all data access operations.</summary>
    private readonly ApplicationDbContext _context;

    /// <summary>
    /// Initializes a new instance of <see cref="AccountRepository"/> with the given
    /// <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The EF Core database context injected by the DI container.</param>
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
        // Materialise the sequence to avoid multiple enumeration inside the LINQ query.
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
        var existing = await _context.Accounts.FindAsync(account.Email.Value);
        if (existing == null) return;
        _context.Entry(existing).CurrentValues.SetValues(ToEntity(account));
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmAccount"/> row to the <see cref="Account"/> domain object,
    /// applying null-coalescing defaults for required fields.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="Account"/> domain object.</returns>
    private static Account ToDomain(OrmAccount e) => Account.Reconstitute(
        e.Email ?? string.Empty, e.HashedPassword ?? string.Empty, e.Nickname ?? string.Empty,
        e.AvatarImagePath, e.Role ?? string.Empty, e.TimeZoneIanaId ?? "UTC",
        e.DefaultMonetaryUnit, e.Locked, e.LoginAttempt, e.EmailConfirmed,
        e.AgreedServiceTerms, e.RegistrationToken, e.ResetPasswordToken,
        e.Created, e.Updated, e.Message, e.Deleted);

    /// <summary>
    /// Maps an <see cref="Account"/> domain object to an ORM <see cref="OrmAccount"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="a">The domain object to convert.</param>
    /// <returns>An <see cref="OrmAccount"/> entity suitable for EF Core tracking.</returns>
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
