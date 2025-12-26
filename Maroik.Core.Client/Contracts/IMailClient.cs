using Maroik.Core.Common.Utilities;

namespace Maroik.Core.Client.Contracts;

public interface IMailClient
{
    /// <summary>
    /// Gets the HttpBody part of account password reset email.
    /// </summary>
    /// <param name="resetPasswordToken"></param>
    /// <param name="title"></param>
    /// <param name="content0"></param>
    /// <param name="content1"></param>
    /// <returns></returns>
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1);
    /// <summary>
    /// Gets the HttpBody part of account verification email.
    /// </summary>
    /// <param name="registrationToken"></param>
    /// <param name="title"></param>
    /// <param name="content0"></param>
    /// <param name="content1"></param>
    /// <returns></returns>
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1);
    /// <summary>
    /// Send email
    /// </summary>
    /// <param name="mail"></param>
    /// <returns></returns>
    public Task<string> SendMailAsync(Mail mail);
}