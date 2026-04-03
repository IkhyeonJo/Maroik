namespace Maroik.Core.Contract.Interfaces;

public interface IMailClient
{
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName);
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName);
    public Task<string> SendMailAsync(string toEmail, string subject, string body, string smtpHost, string smtpPort, string smtpUserName, string smtpPassword, string smtpSSL, string fromEmail, string fromFullName);
}
