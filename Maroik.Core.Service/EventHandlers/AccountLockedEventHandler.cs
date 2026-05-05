using Maroik.Core.Contract.Domains.Account.Events;
using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.EventHandlers;

/// <summary>
/// Sends a lock-notification email to the account owner when <see cref="AccountLockedEvent"/> is dispatched.
/// The email includes a direct link to the forgot-password page so the user can immediately regain access.
/// </summary>
internal sealed class AccountLockedEventHandler : IDomainEventHandler<AccountLockedEvent>
{
    private readonly IMailClient _mailClient;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<AccountLockedEventHandler> _logger;

    public AccountLockedEventHandler(
        IMailClient mailClient,
        IOptions<ServerSetting> settings,
        ILogger<AccountLockedEventHandler> logger)
    {
        _mailClient = mailClient;
        _settings = settings;
        _logger = logger;
    }

    public async Task HandleAsync(AccountLockedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        string domain = _settings.Value.DomainName ?? string.Empty;
        string body = _mailClient.GetMailAccountLockedBody(domain);

        string result = await _mailClient.SendMailAsync(
            toEmail: domainEvent.Email.Value,
            subject: "Your account has been locked",
            body: body,
            smtpHost: _settings.Value.SmtpHost ?? string.Empty,
            smtpPort: _settings.Value.SmtpPort.ToString(),
            smtpUserName: _settings.Value.SmtpUserName ?? string.Empty,
            smtpPassword: _settings.Value.SmtpPassword ?? string.Empty,
            smtpSsl: _settings.Value.SmtpSsl.ToString(),
            fromEmail: _settings.Value.FromEmail ?? string.Empty,
            fromFullName: _settings.Value.FromFullName ?? string.Empty);

        if (result != EnumHelper.GetDescription(AccountMessage.MailSent))
            _logger.LogWarning("Failed to send account-locked notification to {Email}: {Error}", domainEvent.Email.Value, result);
    }
}
