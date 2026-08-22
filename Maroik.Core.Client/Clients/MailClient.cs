using MailKit.Net.Smtp;
using MailKit.Security;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using MimeKit;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Concrete implementation of <see cref="IMailClient"/> that builds transactional email bodies
/// and delivers them via SMTP using MailKit.
/// </summary>
public class MailClient : IMailClient
{
    /// <inheritdoc />
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName)
    {
        // URL-encode the token so special characters survive in the query string
        string url = $"{domainName}Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(registrationToken)}";

        // Return a centred HTML block containing the confirmation link
        return $"""
                <div style='text-align:center;'>
                                        <h1>{title}</h1>
                                        <h3>{content0}</h3>
                                        <h3>{content1}</h3>
                                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                                      </div>
                """;
    }

    /// <inheritdoc />
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName)
    {
        // URL-encode the token so special characters survive in the query string
        string url = $"{domainName}Account/ResetPassword?resetPasswordToken={Uri.EscapeDataString(resetPasswordToken)}";

        // Return a centred HTML block containing the password-reset link
        return $"""
                <div style='text-align:center;'>
                                        <h1>{title}</h1>
                                        <h3>{content0}</h3>
                                        <h3>{content1}</h3>
                                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                                      </div>
                """;
    }

    /// <inheritdoc />
    public async Task<ServiceResult> SendMailAsync(string toEmail, string subject, string body, string smtpHost, string smtpPort, string smtpUserName, string smtpPassword, string smtpSsl, string fromEmail, string fromFullName, CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromFullName, fromEmail));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using SmtpClient smtp = new();
            SecureSocketOptions socketOptions = bool.Parse(smtpSsl) ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await smtp.ConnectAsync(smtpHost, int.Parse(smtpPort), socketOptions, ct);
            await smtp.AuthenticateAsync(smtpUserName, smtpPassword, ct);
            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);

            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            return ServiceResult.Fail(ex.Message);
        }
    }
}
