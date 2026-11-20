using System.Text.Json;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace Maroik.Worker.Workers;

/// <summary>
/// Long-running consumer for the "maroik.class-model-training" queue: each <see cref="TrainClassModelMessage"/> is handed to a scoped
/// <see cref="IClassModelTrainingService"/> (a new DI scope per message, for the DbContext), one at a time (prefetch 1), so no two
/// trainings of an account run at once and a training's memory is not multiplied. Every message is acked once handled — a failed one
/// too (logged at Error): there is no retry or dead-letter queue, since the next suggestion that finds the model out of date asks again.
/// </summary>
public sealed class ClassModelTrainingWorker(
    IConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<ClassModelTrainingWorker> logger) : BackgroundService
{
    /// <summary>The AMQP connection; <see langword="null"/> until connected.</summary>
    private IConnection? _connection;

    /// <summary>The channel the consumer runs on; <see langword="null"/> until connected.</summary>
    private IChannel? _channel;

    /// <summary>Tag of the registered consumer, used by <see cref="StopAsync"/> to cancel it; <see langword="null"/> until consuming.</summary>
    private string? _consumerTag;

    /// <summary>Canceled by <see cref="StopAsync"/>, so a training in progress ends with the host (its request is dropped and asked for again).</summary>
    private readonly CancellationTokenSource _stopping = new();

    /// <summary>First wait between failed initial connection attempts (doubled up to a 30s cap). Overridable in tests.</summary>
    internal TimeSpan ConnectRetryInitialDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>Connects (retrying until the broker is up), then consumes until <paramref name="stoppingToken"/> is canceled.</summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ConnectAndConsumeAsync(stoppingToken);

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Expected during graceful shutdown.
        }
    }

    /// <summary>
    /// Opens the connection and channel, declares the durable training queue and registers the consumer. The broker often is not
    /// reachable the instant the worker starts (both come up together under docker-compose), so the connect is retried with a capped
    /// back-off; afterwards the client's automatic / topology recovery handles a drop.
    /// </summary>
    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        TimeSpan delay = ConnectRetryInitialDelay;
        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                _connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);
                // No arguments: the website's publisher declares the queue the same way (RabbitMqClassModelTrainingPublisher).
                await _channel.QueueDeclareAsync(QueueNames.ClassModelTraining, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += OnMessageReceivedAsync;
                _consumerTag = await _channel.BasicConsumeAsync(QueueNames.ClassModelTraining, autoAck: false, consumer, stoppingToken);

                logger.LogInformation("Class model training consumer started, listening on queue '{Queue}'", QueueNames.ClassModelTraining);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                await CloseAsync();
                logger.LogWarning(ex, "Could not connect the class model training consumer to RabbitMQ; retrying in {DelaySeconds}s", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
                delay = TimeSpan.FromSeconds(Math.Min(30, delay.TotalSeconds * 2));
            }
        }
    }

    /// <summary>Trains for one request, then acks it whatever the outcome.</summary>
    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        TrainClassModelMessage? message = null;
        try
        {
            message = JsonSerializer.Deserialize<TrainClassModelMessage>(ea.Body.Span) ?? throw new JsonException("The message was null.");
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Ignored a class model training request that could not be read (delivery tag {DeliveryTag})", ea.DeliveryTag);
        }

        if (message != null)
        {
            try
            {
                await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<IClassModelTrainingService>().TrainAsync(message.Book, message.AccountEmail, _stopping.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Class model training of the {Book} book of {Email} failed; the request is dropped", message.Book, message.AccountEmail);
            }
        }

        try
        {
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            // The channel dropped: the broker redelivers the request, and training it again is harmless (a current model is left as it is).
            logger.LogWarning(ex, "Could not ack class model training request (delivery tag {DeliveryTag})", ea.DeliveryTag);
        }
    }

    /// <summary>Stops consuming, ends a training in progress and closes the channel and connection.</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null && _consumerTag != null)
        {
            try
            {
                await _channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to cancel the class model training consumer '{ConsumerTag}' during shutdown", _consumerTag);
            }
        }

        await _stopping.CancelAsync();
        await base.StopAsync(cancellationToken);
        await CloseAsync();
    }

    /// <summary>Disposes the channel and the connection, if open.</summary>
    private async Task CloseAsync()
    {
        if (_channel != null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        _stopping.Dispose();
        base.Dispose();
    }
}
