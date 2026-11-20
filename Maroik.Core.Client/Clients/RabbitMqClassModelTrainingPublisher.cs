using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Maroik.Core.Client.Clients;

/// <summary>
/// Publishes <see cref="TrainClassModelMessage"/> to the durable "maroik.class-model-training" queue (no dead-letter policy: a failed
/// training is dropped and asked for again by the next suggestion). The connection handling is <see cref="RabbitMqQueuePublisher"/>'s.
/// </summary>
public sealed class RabbitMqClassModelTrainingPublisher(
    IConnectionFactory factory,
    ILogger<RabbitMqClassModelTrainingPublisher> logger,
    TimeSpan? connectionRecoveryGraceWindow = null)
    : RabbitMqQueuePublisher(factory, logger, QueueNames.ClassModelTraining, () => null, connectionRecoveryGraceWindow), IClassModelTrainingPublisher
{
    /// <inheritdoc />
    public Task PublishAsync(TrainClassModelMessage message, CancellationToken ct = default) => PublishJsonAsync(message, ct);
}
