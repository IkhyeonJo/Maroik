using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for admin-level account management (Management > Account page).
/// Provides full CRUD over user accounts, including forced password changes.
/// </summary>
public interface IManagementAccountService
{
    /// <summary>Returns all accounts for display on the admin account list.</summary>
    Task<List<AccountResponse>> GetAllAccountsAsync(CancellationToken ct = default);

    /// <summary>Returns a single account by email for the admin edit form, or null.</summary>
    Task<AccountResponse?> GetAccountByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Admin-creates a new account (skips the normal email-confirmation flow).</summary>
    Task<ServiceResult> CreateAccountAsync(AccountRequest request, CancellationToken ct = default);

    /// <summary>
    /// Updates an account's details. If <paramref name="newPassword"/> is provided,
    /// hashes and sets it as the account's password.
    /// </summary>
    Task<ServiceResult> UpdateAccountAsync(AccountRequest request, string? newPassword, CancellationToken ct = default);

    /// <summary>Hard-deletes an account and all associated data.</summary>
    Task<ServiceResult> DeleteAccountAsync(string email, CancellationToken ct = default);
}
