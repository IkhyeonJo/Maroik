using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;

namespace Maroik.Worker.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IMailClient"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeMailClient : IMailClient
{
    /// <summary>What <c>GetMailResetPasswordBody</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, string, string, string>? OnGetMailResetPasswordBody { get; set; }

    /// <summary>The arguments of every <c>GetMailResetPasswordBody</c> call, in order.</summary>
    public List<(string ResetPasswordToken, string Title, string Content0, string Content1, string DomainName)> GetMailResetPasswordBodyCalls { get; } = [];

    /// <inheritdoc/>
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName)
    {
        GetMailResetPasswordBodyCalls.Add((resetPasswordToken, title, content0, content1, domainName));
        if (OnGetMailResetPasswordBody is { } on)
        {
            return on(resetPasswordToken, title, content0, content1, domainName);
        }

        return default!;
    }

    /// <summary>What <c>GetMailConfirmationBody</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, string, string, string>? OnGetMailConfirmationBody { get; set; }

    /// <summary>The arguments of every <c>GetMailConfirmationBody</c> call, in order.</summary>
    public List<(string RegistrationToken, string Title, string Content0, string Content1, string DomainName)> GetMailConfirmationBodyCalls { get; } = [];

    /// <inheritdoc/>
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName)
    {
        GetMailConfirmationBodyCalls.Add((registrationToken, title, content0, content1, domainName));
        if (OnGetMailConfirmationBody is { } on)
        {
            return on(registrationToken, title, content0, content1, domainName);
        }

        return default!;
    }

    /// <summary>What <c>GetCalendarReminderBody</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarReminderMail, string, string>? OnGetCalendarReminderBody { get; set; }

    /// <summary>The arguments of every <c>GetCalendarReminderBody</c> call, in order.</summary>
    public List<(CalendarReminderMail Mail, string DomainName)> GetCalendarReminderBodyCalls { get; } = [];

    /// <inheritdoc/>
    public string GetCalendarReminderBody(CalendarReminderMail mail, string domainName)
    {
        GetCalendarReminderBodyCalls.Add((mail, domainName));
        if (OnGetCalendarReminderBody is { } on)
        {
            return on(mail, domainName);
        }

        return default!;
    }

    /// <summary>What <c>SendMailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, ServerSetting, CancellationToken, Task<ServiceResult>>? OnSendMailAsync { get; set; }

    /// <summary>The arguments of every <c>SendMailAsync</c> call, in order.</summary>
    public List<(string ToEmail, string Subject, string Body, ServerSetting Settings, CancellationToken Ct)> SendMailAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> SendMailAsync(string toEmail, string subject, string body, ServerSetting settings, CancellationToken ct)
    {
        SendMailAsyncCalls.Add((toEmail, subject, body, settings, ct));
        if (OnSendMailAsync is { } on)
        {
            return on(toEmail, subject, body, settings, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }
}
