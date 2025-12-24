using Maroik.Core.Common.Utilities;
using Maroik.WebSite.Models.ViewModels.Account;

namespace Maroik.WebSite.Contracts.Clients;

public interface IMailClient
{
    /// <summary>
    /// Gets the HttpBody part of account password reset email.
    /// </summary>
    /// <param name="loginInputViewModel"></param>
    /// <param name="title"></param>
    /// <param name="content0"></param>
    /// <param name="content1"></param>
    /// <returns></returns>
    public string GetMailResetPasswordBody(LoginInputViewModel loginInputViewModel, string title, string content0, string content1);
    /// <summary>
    /// Gets the HttpBody part of account verification email.
    /// </summary>
    /// <param name="loginInputViewModel"></param>
    /// <param name="title"></param>
    /// <param name="content0"></param>
    /// <param name="content1"></param>
    /// <returns></returns>
    public string GetMailConfirmationBody(LoginInputViewModel loginInputViewModel, string title, string content0, string content1);
    /// <summary>
    /// Send email
    /// </summary>
    /// <param name="mail"></param>
    /// <returns></returns>
    public Task<string> SendMailAsync(Mail mail);
}