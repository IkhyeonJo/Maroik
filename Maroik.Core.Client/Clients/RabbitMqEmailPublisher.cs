using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Publishes <see cref="SendEmailMessage"/> to the durable "maroik.email" queue (declared with its dead-letter policy,
/// <see cref="QueueNames.CreateEmailQueueArguments"/>). The connection handling is <see cref="RabbitMqQueuePublisher"/>'s.
/// </summary>
public sealed class RabbitMqEmailPublisher(
    IConnectionFactory factory,
    ILogger<RabbitMqEmailPublisher> logger,
    TimeSpan? connectionRecoveryGraceWindow = null)
    : RabbitMqQueuePublisher(factory, logger, QueueNames.Email, QueueNames.CreateEmailQueueArguments, connectionRecoveryGraceWindow), IEmailPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(SendEmailMessage message, CancellationToken ct = default) => PublishJsonAsync(message, ct);
}
