using Maroik.Core.Contract.Domains.Account.Events;
using Maroik.Core.Contract.Domains.Primitives;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Maroik.Core.Service.EventHandlers;

/// <summary>
/// Sends a welcome email to the newly confirmed account when <see cref="EmailConfirmedEvent"/> is dispatched.
/// </summary>
internal sealed class EmailConfirmedEventHandler : IDomainEventHandler<EmailConfirmedEvent>
{
    private readonly IMailClient _mailClient;
    private readonly IAccountRepository _accountRepository;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<EmailConfirmedEventHandler> _logger;

    public EmailConfirmedEventHandler(
        IMailClient mailClient,
        IAccountRepository accountRepository,
        IOptions<ServerSetting> settings,
        ILogger<EmailConfirmedEventHandler> logger)
    {
        _mailClient = mailClient;
        _accountRepository = accountRepository;
        _settings = settings;
        _logger = logger;
    }

    public async Task HandleAsync(EmailConfirmedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var account = await _accountRepository.GetAccountByEmailAsync(domainEvent.Email.Value);
        string nickname = account?.Nickname ?? domainEvent.Email.Value;

        string body = _mailClient.GetMailWelcomeBody(nickname);

        string result = await _mailClient.SendMailAsync(
            toEmail: domainEvent.Email.Value,
            subject: "Welcome to Maroik!",
            body: body,
            smtpHost: _settings.Value.SmtpHost ?? string.Empty,
            smtpPort: _settings.Value.SmtpPort.ToString(),
            smtpUserName: _settings.Value.SmtpUserName ?? string.Empty,
            smtpPassword: _settings.Value.SmtpPassword ?? string.Empty,
            smtpSsl: _settings.Value.SmtpSsl.ToString(),
            fromEmail: _settings.Value.FromEmail ?? string.Empty,
            fromFullName: _settings.Value.FromFullName ?? string.Empty);

        if (result != EnumHelper.GetDescription(AccountMessage.MailSent))
            _logger.LogWarning("Failed to send welcome email to {Email}: {Error}", domainEvent.Email.Value, result);
    }
}
