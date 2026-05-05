using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Interfaces;
using System.Net.Mail;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Concrete implementation of <see cref="IMailClient"/> that builds transactional email bodies
/// and delivers them via SMTP using <see cref="System.Net.Mail.SmtpClient"/>.
/// </summary>
public class MailClient : IMailClient
{
    /// <inheritdoc />
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName)
    {
        // URL-encode the token so special characters survive in the query string
        string url = $"{domainName}Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(registrationToken)}";

        // Return a centred HTML block containing the confirmation link
        return @$"<div style='text-align:center;'>
                        <h1>{title}<h1>
                        <h3>{content0}<h3>
                        <h3>{content1}<h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
    }

    /// <inheritdoc />
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName)
    {
        // URL-encode the token so special characters survive in the query string
        string url = $"{domainName}Account/ResetPassword?resetPasswordToken={Uri.EscapeDataString(resetPasswordToken)}";

        // Return a centred HTML block containing the password-reset link
        return @$"<div style='text-align:center;'>
                        <h1>{title}<h1>
                        <h3>{content0}<h3>
                        <h3>{content1}<h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
    }

    /// <inheritdoc />
    public string GetMailAccountLockedBody(string domainName)
    {
        string url = $"{domainName}Account/ForgotPassword";
        return @$"<div style='text-align:center;'>
                        <h1>Your account has been locked</h1>
                        <h3>Too many consecutive failed login attempts have triggered an automatic account lock.</h3>
                        <h3>Reset your password using the link below to regain access.</h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
    }

    /// <inheritdoc />
    public string GetMailWelcomeBody(string nickname)
    {
        return @$"<div style='text-align:center;'>
                        <h1>Welcome, {nickname}!</h1>
                        <h3>Your email has been confirmed and your account is now active.</h3>
                        <h3>You can now log in and start using the service.</h3>
                      </div>";
    }

    /// <inheritdoc />
    public async Task<string> SendMailAsync(string toEmail, string subject, string body, string smtpHost, string smtpPort, string smtpUserName, string smtpPassword, string smtpSsl, string fromEmail, string fromFullName)
    {
        try
        {
            // Build the email message
            using MailMessage mailMessage = new();
            mailMessage.From = new MailAddress(fromEmail, fromFullName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true; // Body is an HTML string

            // Configure the SMTP client with credentials and TLS setting from ServerSetting
            using SmtpClient smtp = new(smtpHost, int.Parse(smtpPort));
            smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);
            smtp.EnableSsl = bool.Parse(smtpSsl);

            await smtp.SendMailAsync(mailMessage);

            // Return a localised success message on success
            return EnumHelper.GetDescription(AccountMessage.MailSent);
        }
        catch (Exception ex)
        {
            // Return the raw exception message so the caller can log or display it
            return ex.Message;
        }
    }
}
