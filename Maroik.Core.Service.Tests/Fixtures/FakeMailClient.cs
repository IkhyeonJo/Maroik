using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Settings;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IMailClient"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeMailClient : IMailClient
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>GetMailResetPasswordBody</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, string, string, string>? OnGetMailResetPasswordBody { get; set; }

    /// <summary>The arguments of every <c>GetMailResetPasswordBody</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string ResetPasswordToken, string Title, string Content0, string Content1, string DomainName)> GetMailResetPasswordBodyCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetMailResetPasswordBody</c> added, newest last.</summary>
    private readonly List<(Func<string, string, string, string, string, bool> Match, Func<string, string, string, string, string, string> Answer)> _getMailResetPasswordBodyRules = [];

    /// <summary>Makes every later <c>GetMailResetPasswordBody</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetMailResetPasswordBody(Func<string, string, string, string, string, string> answer) => AnswerGetMailResetPasswordBody((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>GetMailResetPasswordBody</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetMailResetPasswordBody(Func<string, string, string, string, string, bool> match, Func<string, string, string, string, string, string> answer)
    {
        lock (_getMailResetPasswordBodyRules) _getMailResetPasswordBodyRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string GetMailResetPasswordBody(string resetPasswordToken, string title, string content0, string content1, string domainName)
    {
        lock (GetMailResetPasswordBodyCalls) GetMailResetPasswordBodyCalls.Add((resetPasswordToken, title, content0, content1, domainName));
        Func<string, string, string, string, string, string>? answer = null;
        lock (_getMailResetPasswordBodyRules) answer = _getMailResetPasswordBodyRules.LastOrDefault(rule => rule.Match(resetPasswordToken, title, content0, content1, domainName)).Answer;
        if ((answer ?? OnGetMailResetPasswordBody) is { } on)
        {
            return on(resetPasswordToken, title, content0, content1, domainName);
        }

        return Strict ? throw new InvalidOperationException("GetMailResetPasswordBody was called without an answer on a strict fake.") : default!;
    }

    /// <summary>What <c>GetMailConfirmationBody</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, string, string, string>? OnGetMailConfirmationBody { get; set; }

    /// <summary>The arguments of every <c>GetMailConfirmationBody</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string RegistrationToken, string Title, string Content0, string Content1, string DomainName)> GetMailConfirmationBodyCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetMailConfirmationBody</c> added, newest last.</summary>
    private readonly List<(Func<string, string, string, string, string, bool> Match, Func<string, string, string, string, string, string> Answer)> _getMailConfirmationBodyRules = [];

    /// <summary>Makes every later <c>GetMailConfirmationBody</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetMailConfirmationBody(Func<string, string, string, string, string, string> answer) => AnswerGetMailConfirmationBody((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>GetMailConfirmationBody</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetMailConfirmationBody(Func<string, string, string, string, string, bool> match, Func<string, string, string, string, string, string> answer)
    {
        lock (_getMailConfirmationBodyRules) _getMailConfirmationBodyRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string GetMailConfirmationBody(string registrationToken, string title, string content0, string content1, string domainName)
    {
        lock (GetMailConfirmationBodyCalls) GetMailConfirmationBodyCalls.Add((registrationToken, title, content0, content1, domainName));
        Func<string, string, string, string, string, string>? answer = null;
        lock (_getMailConfirmationBodyRules) answer = _getMailConfirmationBodyRules.LastOrDefault(rule => rule.Match(registrationToken, title, content0, content1, domainName)).Answer;
        if ((answer ?? OnGetMailConfirmationBody) is { } on)
        {
            return on(registrationToken, title, content0, content1, domainName);
        }

        return Strict ? throw new InvalidOperationException("GetMailConfirmationBody was called without an answer on a strict fake.") : default!;
    }

    /// <summary>What <c>GetCalendarReminderBody</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CalendarReminderMail, string, string>? OnGetCalendarReminderBody { get; set; }

    /// <summary>The arguments of every <c>GetCalendarReminderBody</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CalendarReminderMail Mail, string DomainName)> GetCalendarReminderBodyCalls { get; } = [];

    /// <summary>The rules <c>AnswerGetCalendarReminderBody</c> added, newest last.</summary>
    private readonly List<(Func<CalendarReminderMail, string, bool> Match, Func<CalendarReminderMail, string, string> Answer)> _getCalendarReminderBodyRules = [];

    /// <summary>Makes every later <c>GetCalendarReminderBody</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetCalendarReminderBody(Func<CalendarReminderMail, string, string> answer) => AnswerGetCalendarReminderBody((_, _) => true, answer);

    /// <summary>Makes a later <c>GetCalendarReminderBody</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerGetCalendarReminderBody(Func<CalendarReminderMail, string, bool> match, Func<CalendarReminderMail, string, string> answer)
    {
        lock (_getCalendarReminderBodyRules) _getCalendarReminderBodyRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public string GetCalendarReminderBody(CalendarReminderMail mail, string domainName)
    {
        lock (GetCalendarReminderBodyCalls) GetCalendarReminderBodyCalls.Add((mail, domainName));
        Func<CalendarReminderMail, string, string>? answer = null;
        lock (_getCalendarReminderBodyRules) answer = _getCalendarReminderBodyRules.LastOrDefault(rule => rule.Match(mail, domainName)).Answer;
        if ((answer ?? OnGetCalendarReminderBody) is { } on)
        {
            return on(mail, domainName);
        }

        return Strict ? throw new InvalidOperationException("GetCalendarReminderBody was called without an answer on a strict fake.") : default!;
    }

    /// <summary>What <c>SendMailAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, string, ServerSetting, CancellationToken, Task<ServiceResult>>? OnSendMailAsync { get; set; }

    /// <summary>The arguments of every <c>SendMailAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string ToEmail, string Subject, string Body, ServerSetting Settings, CancellationToken Ct)> SendMailAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerSendMailAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, string, ServerSetting, CancellationToken, bool> Match, Func<string, string, string, ServerSetting, CancellationToken, Task<ServiceResult>> Answer)> _sendMailAsyncRules = [];

    /// <summary>Makes every later <c>SendMailAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSendMailAsync(Func<string, string, string, ServerSetting, CancellationToken, Task<ServiceResult>> answer) => AnswerSendMailAsync((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>SendMailAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerSendMailAsync(Func<string, string, string, ServerSetting, CancellationToken, bool> match, Func<string, string, string, ServerSetting, CancellationToken, Task<ServiceResult>> answer)
    {
        lock (_sendMailAsyncRules) _sendMailAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<ServiceResult> SendMailAsync(string toEmail, string subject, string body, ServerSetting settings, CancellationToken ct)
    {
        lock (SendMailAsyncCalls) SendMailAsyncCalls.Add((toEmail, subject, body, settings, ct));
        Func<string, string, string, ServerSetting, CancellationToken, Task<ServiceResult>>? answer = null;
        lock (_sendMailAsyncRules) answer = _sendMailAsyncRules.LastOrDefault(rule => rule.Match(toEmail, subject, body, settings, ct)).Answer;
        if ((answer ?? OnSendMailAsync) is { } on)
        {
            return on(toEmail, subject, body, settings, ct);
        }

        return Strict ? throw new InvalidOperationException("SendMailAsync was called without an answer on a strict fake.") : Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => GetMailResetPasswordBodyCalls.Count + GetMailConfirmationBodyCalls.Count + GetCalendarReminderBodyCalls.Count + SendMailAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (GetMailResetPasswordBodyCalls) GetMailResetPasswordBodyCalls.Clear();
        lock (GetMailConfirmationBodyCalls) GetMailConfirmationBodyCalls.Clear();
        lock (GetCalendarReminderBodyCalls) GetCalendarReminderBodyCalls.Clear();
        lock (SendMailAsyncCalls) SendMailAsyncCalls.Clear();
    }
}
