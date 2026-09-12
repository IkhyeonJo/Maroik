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
public sealed class EmailConsumerWorker(
    IConnectionFactory connectionFactory,
    IServiceScopeFactory scopeFactory,
    ILogger<EmailConsumerWorker> logger) : BackgroundService
{
    private IConnection? _connection;
    internal IChannel? Channel;
    private string? _consumerTag;

    // The consumer dispatches one message at a time (RabbitMQ.Client's default
    // ConsumerDispatchConcurrency is 1); prefetch just lets the broker keep the next few buffered
    // at the client so there is no round-trip stall between messages. This still tracks the
    // delivery tag(s) in OnMessageReceivedAsync so StopAsync can wait for the current one to
    // finish ack/nack before the channel is disposed out from under it.
    private readonly ConcurrentDictionary<ulong, byte> _inFlight = new();

    /// <summary>
    /// Establishes the RabbitMQ topology and starts consuming, then runs for the lifetime of the
    /// host; returns only when <paramref name="stoppingToken"/> is canceled.
    /// </summary>
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
    /// Opens the connection and channel, declares the durable email + dead-letter queues, and
    /// registers the consumer. The broker often is not reachable the instant the worker starts
    /// (both come up together under docker-compose), so the initial connect is retried with a
    /// capped back-off rather than letting <see cref="ExecuteAsync"/> fault and stop the host.
    /// Once this returns, the client's automatic/topology recovery handles any later drop.
    /// </summary>
    private async Task ConnectAndConsumeAsync(CancellationToken stoppingToken)
    {
        var delay = TimeSpan.FromSeconds(2);
        var maxDelay = TimeSpan.FromSeconds(30);

        while (true)
        {
            stoppingToken.ThrowIfCancellationRequested();
            try
            {
                _connection = await connectionFactory.CreateConnectionAsync(stoppingToken);
                Channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                // Dead-letter queue first, so it exists before the main queue can route a rejected
                // message to it.
                await Channel.QueueDeclareAsync(
                    queue: QueueNames.EmailDeadLetter,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    cancellationToken: stoppingToken);

                // Arguments must match the Website publisher's declaration of the same queue exactly, or
                // whichever side declares second fails with PRECONDITION_FAILED. The policy dead-letters
                // reject-without-requeue messages to QueueNames.EmailDeadLetter.
                await Channel.QueueDeclareAsync(
                    queue: QueueNames.Email,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: QueueNames.CreateEmailQueueArguments(),
                    cancellationToken: stoppingToken);

                // Cap unacked messages held at the client so one slow SMTP send doesn't let the
                // broker push unbounded messages into this process's memory.
                await Channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(Channel);
                consumer.ReceivedAsync += OnMessageReceivedAsync;

                _consumerTag = await Channel.BasicConsumeAsync(
                    queue: QueueNames.Email,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

#pragma warning disable CA1873
                logger.LogInformation("Email consumer started, listening on queue '{Queue}'", QueueNames.Email);
#pragma warning restore CA1873
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Tear down whatever half-opened so the next attempt starts from a clean slate.
                if (Channel != null)
                {
                    await Channel.DisposeAsync();
                    Channel = null;
                }
                if (_connection != null)
                {
                    await _connection.DisposeAsync();
                    _connection = null;
                }

                logger.LogWarning(ex, "Could not connect to RabbitMQ; retrying in {DelaySeconds}s", delay.TotalSeconds);
                await Task.Delay(delay, stoppingToken);
                delay = TimeSpan.FromSeconds(Math.Min(maxDelay.TotalSeconds, delay.TotalSeconds * 2));
            }
        }
    }

    /// <summary>
    /// Deserializes and processes one delivered message in a fresh DI scope, then acks it.
    /// A failure on first delivery is requeue once (to survive a transient SMTP/network blip);
    /// a failure on the retry nacks without requeue, so a poison message cannot block the queue
    /// forever — the queue's dead-letter policy then routes it to <see cref="QueueNames.EmailDeadLetter"/>
    /// for inspection instead of discarding it. A failure to ack a message that was already
    /// handled successfully is logged and left unacked rather than nacked, since nacking it would
    /// requeue and resend an email that was already delivered (there is no idempotency key to
    /// suppress the duplicate).
    /// </summary>
    internal async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        // DeliveryTag tags every log line for this message even when deserialization fails before
        // a CorrelationId is known, and distinguishes a redelivery of the same message in the log.
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
                    await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
                    var handler = scope.ServiceProvider.GetRequiredService<IEmailMessageHandler>();
                    await handler.HandleAsync(message);
                }

                try
                {
                    await Channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ackEx)
                {
                    // The email was already sent successfully by this point, so this catch must
                    // not fall through to the outer catch's requeue/nack logic below — doing so
                    // would resend an email that already went out.
                    logger.LogError(ackEx, "Failed to ack email message (delivery tag {DeliveryTag}) after a successful send; leaving it unacked", ea.DeliveryTag);
                }
            }
            catch (Exception ex)
            {
                // Give a message one redelivery so a transient SMTP/network blip doesn't permanently
                // drop the email; only give up once it has already come back around once and failed
                // again, so a genuinely poison message still can't block the queue forever.
                if (!ea.Redelivered)
                {
                    logger.LogWarning(ex, "Failed to process email message (delivery tag {DeliveryTag}); requeuing for one retry", ea.DeliveryTag);
                    await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true);
                }
                else
                {
                    // requeue: false triggers the queue's dead-letter policy, so the message is
                    // routed to QueueNames.EmailDeadLetter rather than discarded.
                    logger.LogError(ex, "Failed to process email message (delivery tag {DeliveryTag}) on retry; dead-lettering to '{DeadLetterQueue}'", ea.DeliveryTag, QueueNames.EmailDeadLetter);
                    await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                }
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
                logger.LogWarning(ex, "Failed to cancel RabbitMQ consumer '{ConsumerTag}' during shutdown", _consumerTag);
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
