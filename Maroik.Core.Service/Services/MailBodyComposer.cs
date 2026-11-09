using System.Net;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Services;

/// <inheritdoc cref="IMailBodyComposer"/>
public sealed class MailBodyComposer : IMailBodyComposer
{
    /// <summary>
    /// Joins the configured public base URL to an app-relative path, tolerating a
    /// <c>DomainName</c> configured either with or without a trailing "/". Without it, a value like
    /// "https://maroik.com" (no slash) would produce the
    /// broken link "https://maroik.comAccount/ConfirmEmail".
    /// </summary>
    private static string BuildUrl(string domainName, string relativePath) =>
        $"{domainName.TrimEnd('/')}/{relativePath.TrimStart('/')}";

    /// <summary>
    /// The centered HTML block shared by the confirmation and reset-password mails. Everything interpolated into the markup is
    /// HTML-encoded (the texts come from resx files and the link from configuration today, but nothing here should be able to
    /// turn into markup — or break out of the <c>href</c> attribute — if that ever stops being true).
    /// </summary>
    private static string BuildLinkBody(string title, string content0, string content1, string url)
    {
        string link = WebUtility.HtmlEncode(url);
        return $"""
                <div style='text-align:center;'>
                                        <h1>{WebUtility.HtmlEncode(title)}</h1>
                                        <h3>{WebUtility.HtmlEncode(content0)}</h3>
                                        <h3>{WebUtility.HtmlEncode(content1)}</h3>
                                        <a href='{link}' target='_blank' rel='noopener noreferrer'>{link}</a>
                                      </div>
                """;
    }

    /// <inheritdoc />
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName)
    {
        // URL-encode the token so special characters survive in the query string
        string url = BuildUrl(domainName, $"Account/ConfirmEmail?registrationToken={Uri.EscapeDataString(registrationToken)}");

        // Return a centered HTML block containing the confirmation link
        return BuildLinkBody(title, content0, content1, url);
    }

    /// <inheritdoc />
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName)
    {
        // URL-encode the token so special characters survive in the query string
        string url = BuildUrl(domainName, $"Account/ResetPassword?resetPasswordToken={Uri.EscapeDataString(resetPasswordToken)}");

        // Return a centered HTML block containing the password-reset link
        return BuildLinkBody(title, content0, content1, url);
    }

    /// <inheritdoc />
    public string GetCalendarReminderBody(CalendarReminderMail mail, string domainName)
    {
        string link = WebUtility.HtmlEncode(BuildUrl(domainName, mail.CalendarPath));
        string where = string.IsNullOrWhiteSpace(mail.Location)
            ? ""
            : $"<p>장소 / Where: {WebUtility.HtmlEncode(mail.Location)}</p>";
        return $"""
                <div style='text-align:center;'>
                                        <h1>{WebUtility.HtmlEncode(mail.Title)}</h1>
                                        <h3>일정 알림 / Event reminder</h3>
                                        <p>일시 / When: {WebUtility.HtmlEncode(mail.When)}</p>
                                        {where}
                                        <p>달력 / Calendar: {WebUtility.HtmlEncode(mail.CalendarName)}</p>
                                        <a href='{link}' target='_blank' rel='noopener noreferrer'>{link}</a>
                                      </div>
                """;
    }
}
