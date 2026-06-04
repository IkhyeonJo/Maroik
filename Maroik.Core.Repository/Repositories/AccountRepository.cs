using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmAccount = Maroik.Core.PostgreSQL.Models.Account;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Account"/> domain objects.
/// Maps between the <see cref="Account"/> domain aggregate and the <see cref="OrmAccount"/> PostgreSQL entity.
/// </summary>
public class AccountRepository(ApplicationDbContext context)
    : GenericRepository<Account, OrmAccount>(context), IAccountRepository
{
    protected override DbSet<OrmAccount> Set => Context.Accounts;

    protected override Account ToDomain(OrmAccount e) => Account.Reconstitute(
        e.Email ?? string.Empty, e.HashedPassword ?? string.Empty, e.Nickname ?? string.Empty,
        e.AvatarImagePath, e.Role ?? string.Empty, e.TimeZoneIanaId ?? "UTC",
        e.DefaultMonetaryUnit, e.Locked, e.LoginAttempt, e.EmailConfirmed,
        e.AgreedServiceTerms, e.RegistrationToken, e.ResetPasswordToken,
        e.Created, e.Updated, e.Message, e.Deleted);

    protected override OrmAccount ToEntity(Account a) => new()
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

    public Task<List<Account>> GetAllAsync(CancellationToken ct = default)
        => QueryAllAsync(ct: ct);

    public Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default)
        => QueryFirstAsync(e => e.Email == email, ct);

    public Task<Account?> FindByRegistrationTokenAsync(string token, CancellationToken ct = default)
        => QueryFirstAsync(e => e.RegistrationToken == token, ct);

    public Task<Account?> FindByResetPasswordTokenAsync(string token, CancellationToken ct = default)
        => QueryFirstAsync(e => e.ResetPasswordToken == token, ct);

    public Task<List<Account>> FindByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct = default)
    {
        var list = nicknames.ToList();
        return QueryAsync(e => list.Contains(e.Nickname!), ct: ct);
    }
}
