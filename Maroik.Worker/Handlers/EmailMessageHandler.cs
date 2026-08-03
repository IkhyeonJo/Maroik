using Maroik.Core.Contract.Domains.Account;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Helpers;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messaging;
using Maroik.Core.Contract.Settings;
using Maroik.Worker.Contracts;
using Microsoft.Extensions.Options;

namespace Maroik.Worker.Handlers;

/// <inheritdoc cref="IEmailMessageHandler"/>
public sealed class EmailMessageHandler : IEmailMessageHandler
{
    private readonly IMailClient _mailClient;
    private readonly IAccountRepository _accountRepository;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<EmailMessageHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="EmailMessageHandler"/> with the supplied dependencies.</summary>
    public EmailMessageHandler(
        IMailClient mailClient,
        IAccountRepository accountRepository,
        IOptions<ServerSetting> settings,
        ILogger<EmailMessageHandler> logger)
    {
        _mailClient = mailClient;
        _accountRepository = accountRepository;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(SendEmailMessage message, CancellationToken ct = default)
    {
        ServerSetting settings = _settings.Value;

        string result = await _mailClient.SendMailAsync(
            message.ToEmail,
            message.Subject,
            message.Body,
            settings.SmtpHost ?? "",
            settings.SmtpPort.ToString(),
            settings.SmtpUserName ?? "",
            settings.SmtpPassword ?? "",
            settings.SmtpSsl.ToString(),
            settings.FromEmail ?? "",
            settings.FromFullName ?? "",
            ct);

        if (result == EnumHelper.GetDescription(AccountMessage.MailSent))
        {
            _logger.LogInformation("Email sent to {Email}", message.ToEmail);
            return;
        }

        _logger.LogWarning("SMTP send failed for {Email}: {Result}", message.ToEmail, result);

        Account? account = await _accountRepository.FindByEmailAsync(message.ToEmail, ct);
        if (account == null)
            return;

        account.SetMessage(EnumHelper.GetDescription(AccountMessage.FailToMailSent));
        await _accountRepository.UpdateEntityAsync(account, ct);
    }
}
