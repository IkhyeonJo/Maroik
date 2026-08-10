using System.Text.Json;
using Maroik.Core.Contract.Misc.Messaging;
using Maroik.Worker.Contracts;
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
    private readonly IConnectionFactory _connectionFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailConsumerWorker> _logger;

    private IConnection? _connection;
    internal IChannel? Channel;

    /// <summary>Initializes a new instance of <see cref="EmailConsumerWorker"/> with the supplied dependencies.</summary>
    public EmailConsumerWorker(
        IConnectionFactory connectionFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<EmailConsumerWorker> logger)
    {
        _connectionFactory = connectionFactory;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <summary>
    /// Opens the RabbitMQ connection, declares the durable email queue, and starts consuming.
    /// Runs for the lifetime of the host; returns only when <paramref name="stoppingToken"/> is canceled.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _connection = await _connectionFactory.CreateConnectionAsync(stoppingToken);
        Channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

        await Channel.QueueDeclareAsync(
            queue: QueueNames.Email,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken);

        // Cap in-flight (unacked) messages per consumer so one slow SMTP send doesn't
        // let unbounded messages pile up in memory.
        await Channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(Channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;

        await Channel.BasicConsumeAsync(
            queue: QueueNames.Email,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

#pragma warning disable CA1873
        _logger.LogInformation("Email consumer started, listening on queue '{Queue}'", QueueNames.Email);
#pragma warning restore CA1873

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
    internal async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
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

                await Channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                // Poison message or unexpected failure: drop rather than requeue forever.
                // A dead-letter exchange would be the next step if this needs to be inspected/retried.
                _logger.LogError(ex, "Failed to process email message (delivery tag {DeliveryTag})", ea.DeliveryTag);
                await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
    }

    /// <summary>Stops the base <see cref="BackgroundService"/> and releases the RabbitMQ channel and connection.</summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (Channel != null)
            await Channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}
