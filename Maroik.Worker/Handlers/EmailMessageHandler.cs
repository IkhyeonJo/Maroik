using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Messaging;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Worker.Contracts;
using Microsoft.Extensions.Options;

namespace Maroik.Worker.Handlers;

/// <inheritdoc cref="IEmailMessageHandler"/>
public sealed class EmailMessageHandler : IEmailMessageHandler
{
    private readonly IMailClient _mailClient;
    private readonly IAccountMailStatusService _accountMailStatusService;
    private readonly IOptions<ServerSetting> _settings;
    private readonly ILogger<EmailMessageHandler> _logger;

    /// <summary>Initializes a new instance of <see cref="EmailMessageHandler"/> with the supplied dependencies.</summary>
    public EmailMessageHandler(
        IMailClient mailClient,
        IAccountMailStatusService accountMailStatusService,
        IOptions<ServerSetting> settings,
        ILogger<EmailMessageHandler> logger)
    {
        _mailClient = mailClient;
        _accountMailStatusService = accountMailStatusService;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task HandleAsync(SendEmailMessage message, CancellationToken ct = default)
    {
        ServerSetting settings = _settings.Value;

        ServiceResult result = await _mailClient.SendMailAsync(
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

        if (result.Success)
        {
#pragma warning disable CA1873
            _logger.LogInformation("Email sent to {Email}", message.ToEmail);
#pragma warning restore CA1873
            return;
        }

        _logger.LogWarning("SMTP send failed for {Email}: {Result}", message.ToEmail, result.ErrorKey);

        await _accountMailStatusService.MarkMailSendFailedAsync(message.ToEmail, ct);
    }
}
