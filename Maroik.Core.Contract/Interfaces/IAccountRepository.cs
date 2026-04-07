using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for direct Account table operations.
/// Implementations execute raw EF Core queries; business logic belongs in the service layer.
/// </summary>
public interface IAccountRepository
{
    /// <summary>Returns all account rows (admin use only).</summary>
    public Task<List<AccountResponse>> GetAllAsync();

    /// <summary>Finds an account by its email address (primary key).</summary>
    public Task<AccountResponse?> GetAccountByEmailAsync(string email);

    /// <summary>Finds an account by its email-confirmation token (used during registration).</summary>
    public Task<AccountResponse?> GetAccountByRegistrationTokenAsync(string rawToken);

    /// <summary>Finds an account by its password-reset token.</summary>
    public Task<AccountResponse?> GetAccountByResetPasswordTokenAsync(string rawToken);

    /// <summary>Inserts a new account row into the database.</summary>
    public Task CreateAccountAsync(AccountRequest request);

    /// <summary>Updates all fields of an existing account row.</summary>
    public Task UpdateAccountAsync(AccountRequest request);
}
