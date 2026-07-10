using Maroik.Core.Contract.Domains.Account;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="Account"/> persistence.
/// </summary>
public interface IAccountRepository : IGenericRepository<Account>
{
    /// <summary>Returns all accounts.</summary>
    Task<List<Account>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns the account with the given email, or null if not found.</summary>
    Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Returns the account with the given registration token, or null if not found.</summary>
    Task<Account?> FindByRegistrationTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Returns the account with the given password-reset token, or null if not found.</summary>
    Task<Account?> FindByResetPasswordTokenAsync(string token, CancellationToken ct = default);

    /// <summary>Returns accounts whose nickname matches any of the given values.</summary>
    Task<List<Account>> FindByNicknamesAsync(IEnumerable<string> nicknames, CancellationToken ct = default);
}
