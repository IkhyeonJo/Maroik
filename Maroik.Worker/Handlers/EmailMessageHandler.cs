using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Messaging;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Worker.Contracts;
using Microsoft.Extensions.Options;

namespace Maroik.Worker.Handlers;

/// <inheritdoc cref="IEmailMessageHandler"/>
public sealed class EmailMessageHandler(
    IMailClient mailClient,
    IAccountMailStatusService accountMailStatusService,
    IOptions<ServerSetting> settings,
    ILogger<EmailMessageHandler> logger) : IEmailMessageHandler
{
    /// <inheritdoc />
    public async Task HandleAsync(SendEmailMessage message, CancellationToken ct = default)
    {
        ServiceResult result = await mailClient.SendMailAsync(
            message.ToEmail,
            message.Subject,
            message.Body,
            settings.Value,
            ct);

        if (result.Success)
        {
#pragma warning disable CA1873
            logger.LogInformation("Email sent to {Email}", message.ToEmail);
#pragma warning restore CA1873
            await accountMailStatusService.ClearMailSendFailedAsync(message.ToEmail, ct);
            return;
        }

        logger.LogWarning("SMTP send failed for {Email}: {Result}", message.ToEmail, result.ErrorKey);

        await accountMailStatusService.MarkMailSendFailedAsync(message.ToEmail, ct);

        // Surface the failure to the consumer so it nacks instead of ack a message that was
        // never actually delivered (see IEmailMessageHandler.HandleAsync's documented contract).
        throw new InvalidOperationException($"SMTP send failed for {message.ToEmail}: {result.ErrorKey}");
    }
}
