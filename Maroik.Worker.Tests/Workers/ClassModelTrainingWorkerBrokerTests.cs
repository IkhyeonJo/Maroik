using System.Text.Json;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Maroik.Worker.Tests.Fixtures;
using Maroik.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using RabbitMQ.Client;

namespace Maroik.Worker.Tests.Workers;

/// <summary>
/// <see cref="ClassModelTrainingWorker"/> against a real RabbitMQ broker (Testcontainers; Docker required): it declares the durable
/// training queue, hands each request to the training service (one at a time) and acks it — a failed training too, since the next
/// suggestion that finds the model out of date asks again.
/// </summary>
public class ClassModelTrainingWorkerBrokerTests(WorkerBrokerFixture broker) : IClassFixture<WorkerBrokerFixture>
{
    /// <summary>The current test's cancellation token.</summary>
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    /// <summary>A training service whose behavior is scripted by the test; records each request.</summary>
    private sealed class ScriptedTraining(Func<string, string, Task> onCall) : IClassModelTrainingService
    {
        /// <summary>Every (book, account) handed over, in order (lock it before reading while the worker runs).</summary>
        public List<(string Book, string Email)> Received { get; } = [];

        /// <inheritdoc />
        public async Task TrainAsync(string book, string accountEmail, CancellationToken ct = default)
        {
            lock (Received) Received.Add((book, accountEmail));
            await onCall(book, accountEmail);
        }
    }

    /// <summary>The worker's captured log.</summary>
    private readonly FakeLogger<ClassModelTrainingWorker> _logger = new();

    /// <summary>A worker against the container, resolving <paramref name="training"/>.</summary>
    private ClassModelTrainingWorker CreateWorker(ScriptedTraining training)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IClassModelTrainingService>(training);
        return new ClassModelTrainingWorker(broker.CreateFactory(), services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(), _logger)
        {
            ConnectRetryInitialDelay = TimeSpan.FromMilliseconds(100),
        };
    }

    /// <summary>Recreates the training queue empty, then publishes <paramref name="bodies"/> to it (persistent).</summary>
    private async Task PublishAsync(params byte[][] bodies)
    {
        await using IConnection connection = await broker.CreateFactory().CreateConnectionAsync(Ct);
        await using IChannel channel = await connection.CreateChannelAsync(new CreateChannelOptions(true, true), Ct);
        await channel.QueueDeclareAsync(QueueNames.ClassModelTraining, durable: true, exclusive: false, autoDelete: false, cancellationToken: Ct);
        foreach (byte[] body in bodies)
            await channel.BasicPublishAsync("", QueueNames.ClassModelTraining, mandatory: false, new BasicProperties { Persistent = true }, body, Ct);
    }

    /// <summary>Deletes the training queue.</summary>
    private async Task ResetAsync()
    {
        await using IConnection connection = await broker.CreateFactory().CreateConnectionAsync(Ct);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: Ct);
        await channel.QueueDeleteAsync(QueueNames.ClassModelTraining, ifUnused: false, ifEmpty: false, cancellationToken: Ct);
    }

    /// <summary>The messages (ready and unacked) left on the training queue.</summary>
    private async Task<uint> CountAsync()
    {
        await using IConnection connection = await broker.CreateFactory().CreateConnectionAsync(Ct);
        await using IChannel channel = await connection.CreateChannelAsync(cancellationToken: Ct);
        return (await channel.QueueDeclarePassiveAsync(QueueNames.ClassModelTraining, Ct)).MessageCount;
    }

    /// <summary>Polls <paramref name="condition"/> every 50 ms; throws <see cref="TimeoutException"/> if it is not met within 15 s.</summary>
    private static async Task WaitAsync(Func<bool> condition)
    {
        for (var deadline = DateTime.UtcNow.AddSeconds(15); DateTime.UtcNow < deadline; await Task.Delay(50, Ct))
            if (condition()) return;
        throw new TimeoutException("condition not reached in time");
    }

    /// <summary>The JSON body of a training request.</summary>
    private static byte[] Request(string book, string email) => JsonSerializer.SerializeToUtf8Bytes(new TrainClassModelMessage(book, email));

    /// <summary>
    /// Each request is handed to the training service with its book and account, in order, and acked: nothing is left on the queue.
    /// The worker logs that it listens on the queue.
    /// </summary>
    [Fact]
    public async Task EachRequest_IsTrained_AndAcked()
    {
        await ResetAsync();
        var training = new ScriptedTraining((_, _) => Task.CompletedTask);
        using ClassModelTrainingWorker worker = CreateWorker(training);
        await worker.StartAsync(Ct);
        try
        {
            await PublishAsync(Request("Expenditure", "a@test.com"), Request("Income", "b@test.com"));
            await WaitAsync(() => { lock (training.Received) return training.Received.Count == 2; });
            await WaitAsync(() => CountAsync().GetAwaiter().GetResult() == 0);
        }
        finally
        {
            await worker.StopAsync(Ct);
        }

        Assert.Equal([("Expenditure", "a@test.com"), ("Income", "b@test.com")], training.Received);
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r is { Level: LogLevel.Information, Message: $"Class model training consumer started, listening on queue '{QueueNames.ClassModelTraining}'" });
    }

    /// <summary>
    /// A training that fails is logged at Error with the exception and the request, and acked (dropped): it is not retried in a loop,
    /// and the next request is still trained.
    /// </summary>
    [Fact]
    public async Task AFailedTraining_IsLogged_AndDropped()
    {
        await ResetAsync();
        var training = new ScriptedTraining((_, email) => email == "bad@test.com" ? throw new InvalidOperationException("storage down") : Task.CompletedTask);
        using ClassModelTrainingWorker worker = CreateWorker(training);
        await worker.StartAsync(Ct);
        try
        {
            await PublishAsync(Request("Expenditure", "bad@test.com"), Request("Expenditure", "good@test.com"));
            await WaitAsync(() => { lock (training.Received) return training.Received.Count == 2; });
            await WaitAsync(() => CountAsync().GetAwaiter().GetResult() == 0);
            await Task.Delay(500, Ct); // room for any (wrong) redelivery
        }
        finally
        {
            await worker.StopAsync(Ct);
        }

        Assert.Equal(2, training.Received.Count);
        FakeLogRecord error = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Equal("Class model training of the Expenditure book of bad@test.com failed; the request is dropped", error.Message);
        Assert.IsType<InvalidOperationException>(error.Exception);
    }

    /// <summary>A body that is not a training request never reaches the training service; it is logged at Error and dropped.</summary>
    [Fact]
    public async Task AnUnparsableBody_IsLogged_AndDropped()
    {
        await ResetAsync();
        var training = new ScriptedTraining((_, _) => Task.CompletedTask);
        using ClassModelTrainingWorker worker = CreateWorker(training);
        await worker.StartAsync(Ct);
        try
        {
            await PublishAsync([.. "not json"u8], Request("Income", "a@test.com"));
            await WaitAsync(() => { lock (training.Received) return training.Received.Count == 1; });
            await WaitAsync(() => CountAsync().GetAwaiter().GetResult() == 0);
        }
        finally
        {
            await worker.StopAsync(Ct);
        }

        Assert.Equal([("Income", "a@test.com")], training.Received);
        FakeLogRecord error = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.StartsWith("Ignored a class model training request that could not be read", error.Message, StringComparison.Ordinal);
        Assert.NotNull(error.Exception);
    }
}
