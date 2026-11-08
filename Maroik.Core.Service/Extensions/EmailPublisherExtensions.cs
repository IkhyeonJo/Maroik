using System.Diagnostics;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Microsoft.Extensions.Logging;

namespace Maroik.Core.Service.Extensions;

/// <summary>Publishing a mail from an account flow, where a broker outage is an answer to give, not an exception.</summary>
internal static class EmailPublisherExtensions
{
    extension(IEmailPublisher emailPublisher)
    {
        /// <summary>
        /// Enqueues a mail for asynchronous delivery by Maroik.Worker. Returns <see langword="true"/> once the message has
        /// been accepted by the queue, or <see langword="false"/> (logged) if publishing itself failed (e.g. RabbitMQ unreachable).
        /// </summary>
        internal async Task<bool> TryPublishAsync(ILogger logger, string to, string subject, string body, CancellationToken ct)
        {
            var correlationId = Activity.Current?.Id ?? "";
            try
            {
                logger.LogInformation("Publishing email to {Email}. CorrelationId={CorrelationId}", to, correlationId);
                await emailPublisher.PublishAsync(new SendEmailMessage(to, subject, body, correlationId), ct);
                return true;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed to publish email message for {Email}", to);
                return false;
            }
        }
    }
}
