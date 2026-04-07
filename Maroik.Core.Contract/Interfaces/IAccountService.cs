using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for account-related business logic including login, registration, and password management.
/// </summary>
public interface IAccountService
{
    /// <summary>Returns all accounts (admin use).</summary>
    Task<List<AccountResponse>> GetAllAccountsAsync();

    /// <summary>Returns the account with the given email, or null if not found.</summary>
    Task<AccountResponse?> GetAccountByEmailAsync(string email);

    /// <summary>Validates credentials and returns a <see cref="LoginResult"/> indicating success or failure.</summary>
    Task<LoginResult> LoginAsync(string email, string password);

    /// <summary>Creates a new account and sends a confirmation email. Returns a <see cref="RegisterResult"/> with status details.</summary>
    Task<RegisterResult> RegisterAsync(AccountRequest newAccount, string emailSubject, string emailTitle, string emailContent0, string emailContent1);

    /// <summary>Re-sends the confirmation email to an unverified account.</summary>
    Task<RegisterResult> ResendConfirmationEmailAsync(string email, string emailSubject, string emailTitle, string emailContent0, string emailContent1);

    /// <summary>Activates an account after the user clicks the confirmation link in their email.</summary>
    Task<ConfirmEmailResult> ConfirmEmailAsync(string registrationToken);

    /// <summary>Sends a password-reset email if the address belongs to a valid account.</summary>
    Task<bool> ForgotPasswordAsync(string email, string emailSubject, string emailTitle, string emailContent0, string emailContent1);

    /// <summary>Checks whether the password-reset token is still valid before showing the reset form.</summary>
    Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string resetPasswordToken);

    /// <summary>Sets a new hashed password on the account identified by the reset token.</summary>
    Task<ServiceResult> ResetPasswordAsync(string resetPasswordToken, string newPassword);
}
