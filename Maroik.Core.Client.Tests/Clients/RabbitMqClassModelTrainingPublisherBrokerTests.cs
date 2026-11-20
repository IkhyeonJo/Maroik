using System.Text.Json;
using Maroik.Core.Client.Clients;
using Maroik.Core.Client.Tests.Fixtures;
using Maroik.Core.Contract.Messages;
using Microsoft.Extensions.Logging;
using Moq;
using RabbitMQ.Client;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// <see cref="RabbitMqClassModelTrainingPublisher"/> against a real RabbitMQ broker (Testcontainers; Docker required): a training request
/// lands on the durable training queue as persistent JSON, the shape the worker reads. (The connection handling it shares with the
/// email publisher is pinned by <c>RabbitMqEmailPublisherTests</c> / <c>RabbitMqEmailPublisherBrokerTests</c>.)
/// </summary>
public class RabbitMqClassModelTrainingPublisherBrokerTests(RabbitMqBrokerFixture broker) : IClassFixture<RabbitMqBrokerFixture>
{
    /// <summary>The published training request is on the durable training queue as persistent JSON.</summary>
    [Fact]
    public async Task PublishAsync_PutsAPersistentJsonMessageOnTheTrainingQueue()
    {
        var message = new TrainClassModelMessage("Expenditure", "a@test.com");
        await using IConnection connection = await broker.CreateFactory().CreateConnectionAsync(TestContext.Current.CancellationToken);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: TestContext.Current.CancellationToken);
        await channel.QueueDeleteAsync(QueueNames.ClassModelTraining, ifUnused: false, ifEmpty: false, cancellationToken: TestContext.Current.CancellationToken);
        await using (var publisher = new RabbitMqClassModelTrainingPublisher(broker.CreateFactory(), Mock.Of<ILogger<RabbitMqClassModelTrainingPublisher>>()))
            await publisher.PublishAsync(message, TestContext.Current.CancellationToken);

        BasicGetResult? got = null;
        for (int i = 0; i < 50 && got == null; i++)
        {
            got = await channel.BasicGetAsync(QueueNames.ClassModelTraining, autoAck: true, TestContext.Current.CancellationToken);
            if (got == null) await Task.Delay(100, TestContext.Current.CancellationToken);
        }

        Assert.NotNull(got);
        Assert.Equal(message, JsonSerializer.Deserialize<TrainClassModelMessage>(got.Body.Span));
        Assert.Equal(DeliveryModes.Persistent, got.BasicProperties.DeliveryMode);
        QueueDeclareOk queue = await channel.QueueDeclarePassiveAsync(QueueNames.ClassModelTraining, TestContext.Current.CancellationToken);
        Assert.Equal(QueueNames.ClassModelTraining, queue.QueueName);
    }
}
