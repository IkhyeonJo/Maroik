using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>Self-registration and the e-mail confirmation that activates the account.</summary>
public interface IAccountRegistrationService
{
    /// <summary>
    /// Registers <paramref name="newAccount"/> and queues a confirmation email. A new address creates the
    /// account; an existing unconfirmed one has its registration replaced by this submission (fresh token,
    /// new mail); a confirmed one is reported as already existing. Returns a <see cref="RegisterResult"/>
    /// with status details.
    /// </summary>
    Task<RegisterResult> RegisterAsync(RegisterAccountRequest newAccount, EmailTemplate emailTemplate, CancellationToken ct = default);

    /// <summary>Re-sends the confirmation email to an unverified account.</summary>
    Task<RegisterResult> ResendConfirmationEmailAsync(string email, EmailTemplate emailTemplate, CancellationToken ct = default);

    /// <summary>Checks the mailed confirmation link before the password form is shown; activates nothing.</summary>
    Task<ConfirmEmailResult> ValidateRegistrationTokenAsync(string registrationToken, CancellationToken ct = default);

    /// <summary>
    /// Activates an account only when the mailed confirmation link and the password chosen at
    /// registration are presented together: proof that one person owns both the mailbox and the
    /// credentials, so nobody can pre-register someone else's address and have its owner activate it.
    /// </summary>
    Task<ConfirmEmailResult> ConfirmEmailAsync(string registrationToken, string password, CancellationToken ct = default);
}
