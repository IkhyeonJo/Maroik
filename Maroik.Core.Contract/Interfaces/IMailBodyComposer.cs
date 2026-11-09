using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>Composes what a transactional mail says: its HTML body and links. <see cref="IMailClient"/> only delivers it.</summary>
public interface IMailBodyComposer
{
    /// <summary>
    /// Builds the HTML body for a password-reset email containing the reset link.
    /// </summary>
    /// <param name="resetPasswordToken">The time-limited reset token embedded in the link.</param>
    /// <param name="title">Email heading text.</param>
    /// <param name="content0">First body paragraph.</param>
    /// <param name="content1">Second body paragraph.</param>
    /// <param name="domainName">Application domain used to construct the reset URL.</param>
    string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName);

    /// <summary>
    /// Builds the HTML body for a registration confirmation email containing the verification link.
    /// </summary>
    /// <param name="registrationToken">The time-limited registration token embedded in the link.</param>
    /// <param name="title">Email heading text.</param>
    /// <param name="content0">First body paragraph.</param>
    /// <param name="content1">Second body paragraph.</param>
    /// <param name="domainName">Application domain used to construct the confirmation URL.</param>
    string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName);

    /// <summary>
    /// Builds the HTML body of a calendar reminder e-mail: the event's title, time, place and calendar under Korean and English
    /// labels (an account stores no language), and a link to the owner's calendar page. Every value is HTML-encoded.
    /// </summary>
    /// <param name="mail">What the mail shows.</param>
    /// <param name="domainName">The site's public address, the base of the link.</param>
    string GetCalendarReminderBody(CalendarReminderMail mail, string domainName);
}
