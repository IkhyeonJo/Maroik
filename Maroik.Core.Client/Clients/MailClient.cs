using Maroik.Core.Contract.Utilities;
using Maroik.Core.Contract.Interfaces;
using System.Net.Mail;

namespace Maroik.Core.Client.Clients;

public class MailClient : IMailClient
{
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName)
    {
        string url = $"{domainName}Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(registrationToken)}";
        return @$"<div style='text-align:center;'>
                        <h1>{title}<h1>
                        <h3>{content0}<h3>
                        <h3>{content1}<h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
    }

    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName)
    {
        string url = $"{domainName}Account/ResetPassword?resetPasswordToken={Uri.EscapeDataString(resetPasswordToken)}";
        return @$"<div style='text-align:center;'>
                        <h1>{title}<h1>
                        <h3>{content0}<h3>
                        <h3>{content1}<h3>
                        <a href='{url}' target='_blank' rel='noopener noreferrer'>{url}</a>
                      </div>";
    }

    public async Task<string> SendMailAsync(string toEmail, string subject, string body, string smtpHost, string smtpPort, string smtpUserName, string smtpPassword, string smtpSSL, string fromEmail, string fromFullName)
    {
        try
        {
            using MailMessage mailMessage = new();
            mailMessage.From = new MailAddress(fromEmail, fromFullName);
            mailMessage.To.Add(toEmail);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.IsBodyHtml = true;

            using SmtpClient smtp = new(smtpHost, int.Parse(smtpPort));
            smtp.Credentials = new System.Net.NetworkCredential(smtpUserName, smtpPassword);
            smtp.EnableSsl = bool.Parse(smtpSSL);
            await smtp.SendMailAsync(mailMessage);
            return EnumHelper.GetDescription(AccountMessage.MailSent);
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }
}
