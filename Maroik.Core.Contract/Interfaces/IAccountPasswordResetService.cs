using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>The forgotten-password flow: requesting the mailed link, checking it and setting a new password.</summary>
public interface IAccountPasswordResetService
{
    /// <summary>
    /// Queues a password-reset email if the address belongs to a confirmed, non-deleted account. Always
    /// returns <see langword="true"/>, whatever the outcome, so the response cannot reveal which addresses exist.
    /// </summary>
    Task<bool> ForgotPasswordAsync(string email, EmailTemplate emailTemplate, CancellationToken ct = default);

    /// <summary>Checks whether the password-reset token is still valid before showing the reset form.</summary>
    Task<ResetPasswordValidationResult> ValidateResetPasswordTokenAsync(string resetPasswordToken, CancellationToken ct = default);

    /// <summary>
    /// Sets a new password (policy-checked, then hashed) on the account identified by the reset token,
    /// consuming the token and unlocking the account. On success, <c>SignIn</c> is the account as it now
    /// stands (with its new security stamp) when it may sign in straight away, so the caller can start its
    /// session; it is <see langword="null"/> on failure and when the account may not sign in yet (service
    /// terms not accepted: the same refusal <see cref="IAccountLoginService.LoginAsync"/> gives).
    /// </summary>
    Task<(ServiceResult Result, AccountResponse? SignIn)> ResetPasswordAsync(string resetPasswordToken, string newPassword, CancellationToken ct = default);
}
