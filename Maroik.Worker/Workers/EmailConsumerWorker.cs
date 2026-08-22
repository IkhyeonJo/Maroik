using System.Collections.Concurrent;
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
    private string? _consumerTag;

    // Tracks delivery tags currently being processed, so StopAsync can wait for them to finish
    // acking/nacking before the channel is disposed out from under them.
    private readonly ConcurrentDictionary<ulong, byte> _inFlight = new();

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

        _consumerTag = await Channel.BasicConsumeAsync(
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
            _inFlight[ea.DeliveryTag] = 0;
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
            finally
            {
                _inFlight.TryRemove(ea.DeliveryTag, out _);
            }
        }
    }

    /// <summary>
    /// Cancels the consumer so no new deliveries arrive, stops the base <see cref="BackgroundService"/>,
    /// waits (up to a bounded grace period or the host's own shutdown deadline, whichever comes first)
    /// for any already-in-flight messages to finish being acked/nacked, and then releases the RabbitMQ
    /// channel and connection. Disposing immediately would pull the channel out from under a handler
    /// that is still mid-send, causing its ack/nack to throw and the message to be redelivered and resent.
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Channel != null && _consumerTag != null)
        {
            try
            {
                await Channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cancel RabbitMQ consumer '{ConsumerTag}' during shutdown", _consumerTag);
            }
        }

        await base.StopAsync(cancellationToken);

        using var drainCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        drainCts.CancelAfter(TimeSpan.FromSeconds(25));
        try
        {
            while (!_inFlight.IsEmpty)
                await Task.Delay(100, drainCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Either the host's own shutdown deadline or the 25s drain deadline was reached;
            // proceed to dispose regardless.
        }

        if (Channel != null)
            await Channel.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }
}
