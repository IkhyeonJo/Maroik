namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Client interface for composing and sending transactional emails via SMTP.
/// </summary>
public interface IMailClient
{
    /// <summary>
    /// Builds the HTML body for a password-reset email containing the reset link.
    /// </summary>
    /// <param name="resetPasswordToken">The time-limited reset token embedded in the link.</param>
    /// <param name="title">Email heading text.</param>
    /// <param name="content0">First body paragraph.</param>
    /// <param name="content1">Second body paragraph.</param>
    /// <param name="domainName">Application domain used to construct the reset URL.</param>
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName);

    /// <summary>
    /// Builds the HTML body for a registration confirmation email containing the verification link.
    /// </summary>
    /// <param name="registrationToken">The time-limited registration token embedded in the link.</param>
    /// <param name="title">Email heading text.</param>
    /// <param name="content0">First body paragraph.</param>
    /// <param name="content1">Second body paragraph.</param>
    /// <param name="domainName">Application domain used to construct the confirmation URL.</param>
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName);

    /// <summary>
    /// Builds the HTML body for an account-locked notification email.
    /// Includes a link to the password-reset page so the owner can immediately regain access.
    /// </summary>
    /// <param name="domainName">Application domain used to construct the reset-password URL.</param>
    public string GetMailAccountLockedBody(string domainName);

    /// <summary>
    /// Builds the HTML body for a post-registration welcome email sent after email confirmation.
    /// </summary>
    /// <param name="nickname">The user's display name, included in the greeting.</param>
    public string GetMailWelcomeBody(string nickname);

    /// <summary>
    /// Sends an HTML email via SMTP. Returns an empty string on success or the exception message on failure.
    /// </summary>
    public Task<string> SendMailAsync(string toEmail, string subject, string body, string smtpHost, string smtpPort, string smtpUserName, string smtpPassword, string smtpSsl, string fromEmail, string fromFullName, CancellationToken ct = default);
}
