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

    /// <summary>
    /// Atomically increments the account's failed-login counter in a single UPDATE statement,
    /// locking the account (and setting <paramref name="lockedMessage"/>) if the new count reaches
    /// <paramref name="maxAttempts"/>. Safe under concurrent calls for the same account, unlike a
    /// read-then-write update, since the increment is computed by the database from its own current
    /// row value rather than from a value read earlier by the caller.
    /// </summary>
    Task RecordLoginFailureAsync(string email, int maxAttempts, string lockedMessage, CancellationToken ct = default);
}
