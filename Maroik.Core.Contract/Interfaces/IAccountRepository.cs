using Maroik.Core.Contract.Domains.Account;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for direct Account table operations.
/// Implementations execute raw EF Core queries; business logic belongs in the service layer.
/// </summary>
public interface IAccountRepository
{
    /// <summary>Returns all account rows (admin use only).</summary>
    public Task<List<Account>> GetAllAsync();

    /// <summary>Returns accounts whose nickname matches any of the given values.</summary>
    public Task<List<Account>> GetAccountsByNicknamesAsync(IEnumerable<string> nicknames);

    /// <summary>Finds an account by its email address (primary key).</summary>
    public Task<Account?> GetAccountByEmailAsync(string email);

    /// <summary>Finds an account by its email-confirmation token (used during registration).</summary>
    public Task<Account?> GetAccountByRegistrationTokenAsync(string rawToken);

    /// <summary>Finds an account by its password-reset token.</summary>
    public Task<Account?> GetAccountByResetPasswordTokenAsync(string rawToken);

    /// <summary>Inserts a new account row into the database.</summary>
    public Task CreateAccountAsync(Account account);

    /// <summary>Updates all fields of an existing account row.</summary>
    public Task UpdateAccountAsync(Account account);
}
