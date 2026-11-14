using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>
/// Stand-in for the RabbitMQ class-model training publisher, registered by <see cref="MaroikWebApplicationFactory"/>: instead of queuing
/// the request for the worker it runs the worker's real training (<see cref="IClassModelTrainingService"/>) at once, against the test
/// database, so a website test sees the flow end to end — the first suggestion asks for the model, the next one is answered by it.
/// </summary>
public sealed class InlineClassModelTrainingPublisher(IServiceScopeFactory scopes) : IClassModelTrainingPublisher
{
    /// <inheritdoc />
    public async Task PublishAsync(TrainClassModelMessage message, CancellationToken ct = default)
    {
        using IServiceScope scope = scopes.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IClassModelTrainingService>().TrainAsync(message.Book, message.AccountEmail, ct);
    }
}
