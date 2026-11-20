using Maroik.Core.Contract.Messages;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Publishes <see cref="TrainClassModelMessage"/> to a durable queue, so a class-suggestion model is trained by Maroik.Worker instead of
/// in the website's request.
/// </summary>
public interface IClassModelTrainingPublisher
{
    /// <summary>Enqueues a training request. Throws if the message could not be enqueued.</summary>
    Task PublishAsync(TrainClassModelMessage message, CancellationToken ct = default);
}
