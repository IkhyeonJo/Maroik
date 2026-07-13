using System.Text.Json;
using Maroik.Core.Contract.Messaging;
using Maroik.Worker.Handlers;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace Maroik.Worker.Workers;

/// <summary>
/// Long-running consumer for the "maroik.email" queue.
/// Connects to RabbitMQ, dequeues <see cref="SendEmailMessage"/> items, and delegates
/// each one to a scoped <see cref="IEmailMessageHandler"/> (a new DI scope per message,
/// since the handler depends on scoped services such as the EF Core DbContext).
/// </summary>
public sealed class EmailConsumerWorker : BackgroundService
{
    private readonly string _connectionString;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailConsumerWorker> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    /// <summary>Initializes a new instance of <see cref="EmailConsumerWorker"/> with the supplied dependencies.</summary>
    public EmailConsumerWorker(
        IConfiguration configuration,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailConsumerWorker> logger)
    {
        _connectionString = configuration.GetConnectionString("RabbitMQ") ?? "amqp://guest:guest@localhost:5672/";
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Opens the RabbitMQ connection, declares the durable email queue, and starts consuming.
    /// Runs for the lifetime of the host; returns only when <paramref name="stoppingToken"/> is cancelled.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
        _connection = await factory.CreateConnectionAsync(stoppingToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await _channel.QueueDeclareAsync(
            queue: QueueNames.Email,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Cap in-flight (unacked) messages per consumer so one slow SMTP send doesn't
        // let unbounded messages pile up in memory.
        await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await _channel.BasicConsumeAsync(
            queue: QueueNames.Email,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        _logger.LogInformation("Email consumer started, listening on queue '{Queue}'", QueueNames.Email);

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
    /// Deserializes and processes one delivered message in a fresh DI scope, then acks it.
    /// Any failure (bad JSON, handler exception) nacks without requeue, so a single poison
    /// message cannot block the queue forever.
    /// </summary>
    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        // DeliveryTag alone keeps concurrently-processed messages (prefetch 10) distinguishable
        // in the logs even if deserialization fails before a CorrelationId is known.
        using (LogContext.PushProperty("DeliveryTag", ea.DeliveryTag))
        {
            try
            {
                SendEmailMessage message = JsonSerializer.Deserialize<SendEmailMessage>(ea.Body.Span)
                    ?? throw new InvalidOperationException("Deserialized email message was null");

                // Threads the originating Website request's id through so this message's logs
                // can be traced back to the request that published it.
                using (LogContext.PushProperty("CorrelationId", message.CorrelationId))
                {
                    await using AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IEmailMessageHandler>();
                    await handler.HandleAsync(message);
                }

                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Poison message or unexpected failure: drop rather than requeue forever.
                // A dead-letter exchange would be the next step if this needs to be inspected/retried.
                _logger.LogError(ex, "Failed to process email message (delivery tag {DeliveryTag})", ea.DeliveryTag);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }

    /// <summary>Stops the base <see cref="BackgroundService"/> and releases the RabbitMQ channel and connection.</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_channel != null)
            await _channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}
