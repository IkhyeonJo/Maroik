using System.Net.Security;
using MailKit.Net.Smtp;
using MailKit.Security;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Concrete implementation of <see cref="IMailClient"/> that builds transactional email bodies
/// and delivers them via SMTP using MailKit.
/// </summary>
public class MailClient(ILogger<MailClient> logger) : IMailClient
{
    /// <summary>
    /// Joins the configured public base URL to an app-relative path, tolerating a
    /// <c>DomainName</c> configured either with or without a trailing "/". Previously the callers
    /// assumed a trailing slash and a value like "https://maroik.com" (no slash) produced the
    /// broken link "https://maroik.comAccount/ConfirmEmail".
    /// </summary>
    private static string BuildUrl(string domainName, string relativePath) =>
        $"{(domainName).TrimEnd('/')}/{relativePath.TrimStart('/')}";

    /// <inheritdoc />
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName)
    {
        // URL-encode the token so special characters survive in the query string
        string url = BuildUrl(domainName, $"Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(registrationToken)}");

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
        string url = BuildUrl(domainName, $"Account/ResetPassword?resetPasswordToken={Uri.EscapeDataString(resetPasswordToken)}");

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
    public async Task<ServiceResult> SendMailAsync(string toEmail, string subject, string body, ServerSetting settings, CancellationToken ct = default)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(settings.FromFullName ?? "", settings.FromEmail ?? ""));
            message.To.Add(new MailboxAddress("", toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = body }.ToMessageBody();

            using SmtpClient smtp = new();
            // Bounded well under the worker's shutdown drain budget so a hung connection
            // can't outlive it and get killed mid-send (which would cause a duplicate
            // send on redelivery). MailKit's default is ~100s.
            smtp.Timeout = 20_000;
            // SmtpSsl on  => negotiate the best available (implicit TLS or STARTTLS) AND validate the
            //               server certificate normally.
            // SmtpSsl off => still upgrade opportunistically via STARTTLS when the server advertises
            //               it (so credentials are never sent in the clear against a relay that
            //               offers TLS). The certificate check is relaxed only for an untrusted /
            //               self-signed chain — the realistic "operator's own relay" case. A
            //               hostname mismatch or a missing certificate is still rejected, since both
            //               are the signature of an active man-in-the-middle rather than a benign
            //               self-signed relay.
            SecureSocketOptions socketOptions = settings.SmtpSsl
                ? SecureSocketOptions.Auto
                : SecureSocketOptions.StartTlsWhenAvailable;
            if (!settings.SmtpSsl)
            {
                smtp.ServerCertificateValidationCallback = (_, _, _, errors) =>
                {
                    switch (errors)
                    {
                        case SslPolicyErrors.None:
                            return true;
                        // Accept only a bare chain / self-signed-root problem; reject name mismatch and
                        // "no certificate" outright.
                        case SslPolicyErrors.RemoteCertificateChainErrors:
                            logger.LogWarning(
                                "Accepting an untrusted SMTP server certificate for {SmtpHost} because SmtpSsl is disabled for this relay",
                                settings.SmtpHost);
                            return true;
                        case SslPolicyErrors.RemoteCertificateNotAvailable:
                        case SslPolicyErrors.RemoteCertificateNameMismatch:
                        default:
                            logger.LogWarning(
                                "Rejecting the SMTP server certificate for {SmtpHost}: {SslPolicyErrors}",
                                settings.SmtpHost, errors);
                            return false;
                    }
                };
            }
            await smtp.ConnectAsync(settings.SmtpHost ?? "", settings.SmtpPort, socketOptions, ct);
            await smtp.AuthenticateAsync(settings.SmtpUserName ?? "", settings.SmtpPassword ?? "", ct);
            await smtp.SendAsync(message, ct);
            await smtp.DisconnectAsync(true, ct);

            return ServiceResult.Ok();
        }
        catch (Exception ex)
        {
            // Log the detail; return a generic message so raw SMTP/exception text (server names,
            // internal hosts) never reaches a caller that might surface it to a user.
            logger.LogError(ex, "Failed to send mail to {ToEmail}", toEmail);
            return ServiceResult.Failure("Mail.SendFailed", "Failed to send the email.");
        }
    }
}
