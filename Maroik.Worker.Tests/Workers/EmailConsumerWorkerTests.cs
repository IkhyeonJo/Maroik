using System.Globalization;
using System.Text;
using System.Text.Json;
using Maroik.Core.Client.Tests.Fixtures;
using Maroik.Core.Contract.Messages;
using Maroik.Worker.Interfaces;
using Maroik.Worker.Tests.Fixtures;
using Maroik.Worker.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
// ReSharper disable AccessToDisposedClosure

namespace Maroik.Worker.Tests.Workers;

/// <summary>
/// Unit tests for <see cref="EmailConsumerWorker.OnMessageReceivedAsync"/> — the per-message
/// ack/nack decision that keeps a single poison message from blocking the "maroik.email" queue.
/// The channel is injected directly (via the internal <c>_channel</c> field, exposed to this
/// assembly through <see cref="System.Runtime.CompilerServices.InternalsVisibleToAttribute"/>)
/// so the connection/consume loop in <c>ExecuteAsync</c> doesn't need to run.
/// </summary>
public class EmailConsumerWorkerTests
{
    /// <summary>The message most tests deliver.</summary>
    private static readonly SendEmailMessage _message = new("to@test.com", "Subject", "Body", "corr-1");

    /// <summary>A scope factory whose scopes resolve <paramref name="handler"/>.</summary>
    private static IServiceScopeFactory BuildScopeFactory(IEmailMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddSingleton(handler);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    /// <summary>
    /// A worker resolving <paramref name="handler"/> with a fake channel already attached (returned in <paramref name="channel"/>;
    /// the worker owns it from here and disposes it with itself).
    /// </summary>
    private static EmailConsumerWorker CreateSut(IEmailMessageHandler handler, out FakeChannel channel, ILogger<EmailConsumerWorker>? logger = null)
    {
        var worker = new EmailConsumerWorker(new FakeConnectionFactory(), BuildScopeFactory(handler), logger ?? NullLogger<EmailConsumerWorker>.Instance);
        channel = new FakeChannel();
        worker.Channel = channel;
        return worker;
    }

    /// <param name="redelivered"></param>
    /// <param name="retryCount">
    /// Sets <see cref="EmailConsumerWorker.RetryCountHeader"/> on the delivery's properties —
    /// this worker's own retry counter, tracked in a republished header rather than via
    /// <c>BasicDeliverEventArgs.Redelivered</c> (see that constant's doc comment for why).
    /// </param>
    /// <param name="body"></param>
    /// <param name="deliveryTag"></param>
    private static BasicDeliverEventArgs MakeDeliverArgs(byte[] body, ulong deliveryTag = 1, bool redelivered = false, int retryCount = 0)
    {
        var properties = new BasicProperties();
        if (retryCount > 0)
            properties.Headers = new Dictionary<string, object?> { [EmailConsumerWorker.RetryCountHeader] = retryCount };

        return new BasicDeliverEventArgs("consumer-tag", deliveryTag, redelivered, exchange: "", routingKey: QueueNames.Email,
            properties, body, CancellationToken.None);
    }

    /// <summary>On message received async valid message invokes handler and acks.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_ValidMessage_InvokesHandlerAndAcks()
    {
        var handler = new FakeEmailMessageHandler();
        using var worker = CreateSut(handler, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 7));

        Assert.Equal(_message, Assert.Single(handler.HandleAsyncCalls).Message);
        Assert.Equal([(7UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
        Assert.Empty(channel.BasicNackAsyncCalls);
    }

    /// <summary>
    /// The handler's entries carry the message's delivery tag and correlation id as scopes (so a mail's log lines can be traced back
    /// to the website request that queued it), and the worker's own entries about the message carry its delivery tag.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_LogsInsideTheDeliveryTagAndCorrelationIdScopes()
    {
        using var loggerProvider = new FakeLoggerProvider();
        using var loggerFactory = new LoggerFactory([loggerProvider]);
        ILogger handlerLogger = loggerFactory.CreateLogger("Handler");
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) =>
        {
            handlerLogger.LogInformation("Sending");
            return Task.CompletedTask;
        };
        using var worker = CreateSut(handler, out var channel, loggerFactory.CreateLogger<EmailConsumerWorker>());
        channel.OnBasicAckAsync = (_, _, _) => ValueTask.FromException(new InvalidOperationException("channel closed"));

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(JsonSerializer.SerializeToUtf8Bytes(_message), deliveryTag: 7));

        IReadOnlyList<FakeLogRecord> records = loggerProvider.Collector.GetSnapshot();
        Assert.Equal(["DeliveryTag: 7", "CorrelationId: corr-1"], Assert.Single(records, r => r.Category == "Handler").Scopes.Select(s => s?.ToString()));
        Assert.Equal(["DeliveryTag: 7"], Assert.Single(records, r => r.Level == LogLevel.Error).Scopes.Select(s => s?.ToString()));
    }

    /// <summary>On message received async invalid JSON on first delivery republishes for retry (ack, not nack) and never calls handler.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_InvalidJson_FirstDelivery_RepublishesForRetryAndNeverCallsHandler()
    {
        var handler = new FakeEmailMessageHandler();
        using var worker = CreateSut(handler, out var channel);
        byte[] body = Encoding.UTF8.GetBytes("not valid json");

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 3, redelivered: false));

        Assert.Empty(handler.HandleAsyncCalls);
        var publish = Assert.Single(channel.BasicPublishAsyncCalls);
        Assert.Equal(("", EmailConsumerWorker.RetryQueueName(1), false), (publish.Exchange, publish.RoutingKey, publish.Mandatory));
        var published = Assert.IsType<BasicProperties>(publish.BasicProperties);
        Assert.Equal(1, published.Headers![EmailConsumerWorker.RetryCountHeader]);
        Assert.Equal("30000", published.Expiration); // the first retry waits 30 s in the first retry queue
        Assert.Equal([(3UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
        Assert.Empty(channel.BasicNackAsyncCalls);
    }

    /// <summary>On message received async invalid JSON already retried MaxRetries times nacks without requeue and never calls handler.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_InvalidJson_AlreadyRetried_NacksWithoutRequeueAndNeverCallsHandler()
    {
        var handler = new FakeEmailMessageHandler();
        using var worker = CreateSut(handler, out var channel);
        byte[] body = Encoding.UTF8.GetBytes("not valid json");

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 3, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries));

        Assert.Empty(handler.HandleAsyncCalls);
        Assert.Equal([(3UL, false, false)], channel.BasicNackAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple, c.Requeue)));
        Assert.Empty(channel.BasicAckAsyncCalls);
    }

    /// <summary>On message received async null JSON body on first delivery republishes for retry.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NullJsonBody_FirstDelivery_RepublishesForRetry()
    {
        var handler = new FakeEmailMessageHandler();
        using var worker = CreateSut(handler, out var channel);
        byte[] body = "null"u8.ToArray();

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 4, redelivered: false));

        Assert.Equal([(4UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
        Assert.Empty(channel.BasicNackAsyncCalls);
    }

    /// <summary>On message received async null JSON body already retried MaxRetries times nacks without requeue.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NullJsonBody_AlreadyRetried_NacksWithoutRequeue()
    {
        var handler = new FakeEmailMessageHandler();
        using var worker = CreateSut(handler, out var channel);
        byte[] body =
        [
            .. "null"u8
        ];

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 4, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries));

        Assert.Equal([(4UL, false, false)], channel.BasicNackAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple, c.Requeue)));
    }

    /// <summary>The single entry <paramref name="logger"/> recorded at <paramref name="level"/> whose text contains <paramref name="text"/>.</summary>
    private static FakeLogRecord SingleLog(FakeLogger<EmailConsumerWorker> logger, LogLevel level, string text) =>
        Assert.Single(logger.Collector.GetSnapshot(), r => r.Level == level && r.Message.Contains(text, StringComparison.Ordinal));

    /// <summary>On message received async handler throws on first delivery republishes for retry, giving a transient SMTP blip a second chance.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_FirstDelivery_RepublishesForRetry()
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = CreateSut(handler, out var channel, logger);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 9, redelivered: false));

        Assert.Equal([(9UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
        Assert.Empty(channel.BasicNackAsyncCalls);
        FakeLogRecord warning = SingleLog(logger, LogLevel.Warning, "Failed to process email message (delivery tag 9, retry");
        Assert.IsType<InvalidOperationException>(warning.Exception);
    }

    /// <summary>
    /// Each retry waits longer: a delivery that has already been retried once goes to the second retry queue with the second
    /// delay (5 minutes), its retry header bumped to 2.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_OnTheFirstRetry_WaitsTheSecondDelayInTheSecondRetryQueue()
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        using var worker = CreateSut(handler, out var channel);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(JsonSerializer.SerializeToUtf8Bytes(_message), deliveryTag: 21, redelivered: false, retryCount: 1));

        var publish = Assert.Single(channel.BasicPublishAsyncCalls);
        Assert.Equal(EmailConsumerWorker.RetryQueueName(2), publish.RoutingKey);
        Assert.Equal("300000", publish.BasicProperties.Expiration);
        Assert.Equal(2, publish.BasicProperties.Headers![EmailConsumerWorker.RetryCountHeader]);
        Assert.Equal([(21UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
    }

    /// <summary>On message received async, when the handler succeeds but the ack itself throws, the message is neither nacked nor re-queued — it must not be resent since the email already went out.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_AckThrowsAfterSuccessfulHandle_DoesNotNack()
    {
        var handler = new FakeEmailMessageHandler();
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = CreateSut(handler, out var channel, logger);
        channel.OnBasicAckAsync = (_, _, _) => ValueTask.FromException(new InvalidOperationException("channel closed"));
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 11));

        Assert.NotNull(SingleLog(logger, LogLevel.Error, "Failed to ack email message (delivery tag 11)").Exception);

        Assert.Equal(_message, Assert.Single(handler.HandleAsyncCalls).Message);
        Assert.Empty(channel.BasicNackAsyncCalls);
    }

    /// <summary>
    /// Regression test: if the failure-path <c>BasicNackAsync</c> call itself throws (e.g. the
    /// channel is mid-recovery), that exception must be caught rather than escaping
    /// <c>OnMessageReceivedAsync</c> unhandled -- the success-path ack already has this protection,
    /// and the nack path needs the same so a broker blip during error handling doesn't surface as
    /// an unobserved/unhandled exception from the message loop.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_NackThrowsAfterHandlerFailure_DoesNotThrow()
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = CreateSut(handler, out var channel, logger);
        channel.OnBasicNackAsync = (_, _, _, _) => ValueTask.FromException(new InvalidOperationException("channel closed"));
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        var exception = await Record.ExceptionAsync(() =>
            worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 13, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries)));

        Assert.Null(exception);
        Assert.NotNull(SingleLog(logger, LogLevel.Error, "Failed to republish/ack or nack email message (delivery tag 13)").Exception);
    }

    /// <summary>
    /// Regression test: if the retry-path <c>BasicPublishAsync</c> (or the ack that follows it)
    /// itself throws (e.g. the channel is mid-recovery), that exception must be caught rather than
    /// escaping <c>OnMessageReceivedAsync</c> unhandled.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_RepublishThrowsAfterHandlerFailure_DoesNotThrow()
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        using var worker = CreateSut(handler, out var channel);
        channel.OnBasicPublishAsync = (_, _, _, _, _, _) => ValueTask.FromException(new InvalidOperationException("channel closed"));
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        var exception = await Record.ExceptionAsync(() =>
            worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 14, redelivered: false)));

        Assert.Null(exception);
    }

    /// <summary>
    /// Regression test: a send still running when the shutdown drain deadline is reached must be
    /// canceled (via the token every <c>HandleAsync</c> call is given), not left to race the
    /// channel disposal that follows <c>StopAsync</c>'s drain wait -- disposing the channel out
    /// from under a still-running send would throw from its own ack/nack and force a
    /// redelivery/resend of a send that may already have completed.
    /// </summary>
    [Fact]
    public async Task StopAsync_CancelsInFlightSend_WhenDrainDeadlineReached()
    {
        var handlerStarted = new TaskCompletionSource();
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = async (_, ct) =>
        {
            handlerStarted.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
        };
        using var worker = CreateSut(handler, out var channel);
        worker.DrainTimeout = TimeSpan.FromMilliseconds(50);
        worker.CancelUnwindTimeout = TimeSpan.FromMilliseconds(500);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        Task received = worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 21, redelivered: false));
        await handlerStarted.Task;

        // StopAsync itself must return (the unwind wait isn't left hanging on the now-canceled
        // send), and OnMessageReceivedAsync's own catch -- not an unhandled exception -- must be
        // what observes the cancellation: it never rethrows, it republishes for a first retry
        // (this is a first-attempt failure, so it isn't dead-lettered yet) and acks the original.
        await worker.StopAsync(CancellationToken.None);
        await received;

        Assert.Equal([(21UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
        Assert.Empty(channel.BasicNackAsyncCalls);
    }

    /// <summary>On message received async handler throws after already being retried MaxRetries times nacks without requeue, so a genuinely poison message can't block the queue forever.</summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_AlreadyRetried_NacksWithoutRequeue()
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = CreateSut(handler, out var channel, logger);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 9, redelivered: true, retryCount: EmailConsumerWorker.MaxRetries));

        Assert.Equal([(9UL, false, false)], channel.BasicNackAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple, c.Requeue)));
        Assert.NotNull(SingleLog(logger, LogLevel.Error, $"Failed to process email message (delivery tag 9) after {EmailConsumerWorker.MaxRetries}").Exception);
        Assert.Empty(channel.BasicAckAsyncCalls);
    }

    /// <summary>
    /// Regression test: a message that the broker marks <c>Redelivered</c> for a reason unrelated
    /// to this worker's own retry decision (e.g. a connection drop right after this process started,
    /// before it ever attempted the message) must still get its full retry budget — the decision is
    /// keyed on <see cref="EmailConsumerWorker.RetryCountHeader"/>, not <c>Redelivered</c>.
    /// </summary>
    [Fact]
    public async Task OnMessageReceivedAsync_HandlerThrows_RedeliveredButNoRetryHeader_StillRepublishesForRetry()
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        using var worker = CreateSut(handler, out var channel);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);

        // redelivered: true, but no retry header -- simulates a broker-level redelivery this
        // consumer never itself attempted (e.g. a fresh process after a crash).
        await worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 10, redelivered: true));

        Assert.Equal([(10UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
        Assert.Empty(channel.BasicNackAsyncCalls);
    }

    // -- Connect / topology / consume loop -----------------------------------------------------

    /// <summary>BackgroundService starts <c>ExecuteAsync</c> without awaiting it, so wait (bounded) for the observable effect instead of sleeping.</summary>
    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
            await Task.Delay(5, timeout.Token);
    }

    /// <summary>
    /// Waits until the worker has logged that it is consuming. <see cref="BrokerFixture.Consume"/> is set inside the
    /// <c>BasicConsume</c> call, before the worker has stored the consumer tag it returns, so a test that stops the
    /// worker as soon as that is set can find no tag to cancel yet; the log is written after the tag is stored.
    /// </summary>
    private static Task WaitUntilConsumingAsync(FakeLogger<EmailConsumerWorker> logger) =>
        WaitUntilAsync(() => logger.Collector.GetSnapshot().Any(r =>
            r.Level == LogLevel.Information && r.Message.Contains("Email consumer started", StringComparison.Ordinal)));

    /// <summary>Fake connection factory, connection and channel that record the worker's startup calls.</summary>
    private sealed class BrokerFixture : IDisposable
    {
        /// <summary>Connection factory handed to the worker; returns <see cref="Connection"/>.</summary>
        public FakeConnectionFactory Factory { get; } = new();
        /// <summary>Connection whose channel is <see cref="Channel"/>.</summary>
        public FakeConnection Connection { get; } = new();
        /// <summary>Channel whose declare / QoS / consume calls are recorded.</summary>
        public FakeChannel Channel { get; } = new();
        /// <summary>Channel calls in the order the worker made them (<c>declare:…</c>, <c>qos</c>, <c>consume</c>).</summary>
        public List<string> Calls { get; } = [];
        /// <summary>Arguments the email queue was declared with.</summary>
        public IDictionary<string, object?>? EmailQueueArguments { get; private set; }
        /// <summary>The options the worker opened its channel with.</summary>
        public CreateChannelOptions? ChannelOptions { get; private set; }
        /// <summary>The last <c>BasicQos</c> settings.</summary>
        public (uint PrefetchSize, ushort PrefetchCount, bool Global)? Qos { get; private set; }
        /// <summary>The queue and auto-ack flag of the last <c>BasicConsume</c>.</summary>
        public (string Queue, bool AutoAck)? Consume { get; private set; }

        /// <summary>Wires the factory to return <see cref="Connection"/> and the connection to hand out <see cref="Channel"/>.</summary>
        public BrokerFixture()
        {
            Factory.OnCreateConnectionAsync = _ => Task.FromResult<IConnection>(Connection);
            Connection.OnCreateChannelAsync = (options, _) =>
            {
                ChannelOptions = options;
                return Task.FromResult<IChannel>(Channel);
            };
            Channel.OnQueueDeclareAsync = (queue, durable, exclusive, autoDelete, args, _, _, _) =>
            {
                Calls.Add($"declare:{queue}:durable={durable}:exclusive={exclusive}:autoDelete={autoDelete}");
                if (queue == QueueNames.Email) EmailQueueArguments = args;
                return Task.FromResult(new QueueDeclareOk(queue, 0, 0));
            };
            Channel.OnBasicQosAsync = (size, count, global, _) =>
            {
                Calls.Add("qos");
                Qos = (size, count, global);
                return Task.CompletedTask;
            };
            Channel.OnBasicConsumeAsync = (queue, autoAck, _, _, _, _, _, _) =>
            {
                Calls.Add("consume");
                Consume = (queue, autoAck);
                return Task.FromResult("consumer-tag-1");
            };
        }

        /// <summary>Creates a worker wired to this fake broker, with short retry and drain timeouts.</summary>
        public EmailConsumerWorker CreateWorker(ILogger<EmailConsumerWorker>? logger = null) =>
            new(Factory, BuildScopeFactory(new FakeEmailMessageHandler()), logger ?? NullLogger<EmailConsumerWorker>.Instance)
        {
            ConnectRetryInitialDelay = TimeSpan.FromMilliseconds(10),
            DrainTimeout = TimeSpan.FromMilliseconds(50),
        };

        /// <inheritdoc />
        public void Dispose()
        {
            Channel.Dispose();
            Connection.Dispose();
        }
    }

    /// <summary>
    /// Startup declares the dead-letter queue BEFORE the main queue (so the main queue's dead-letter
    /// policy has somewhere to route), with the exact arguments the Website publisher uses (a mismatch
    /// makes whichever side declares second fail with PRECONDITION_FAILED), caps unacked deliveries, and
    /// consumes with manual acknowledgements.
    /// </summary>
    [Fact]
    public async Task StartAsync_DeclaresDeadLetterQueueFirst_SetsPrefetch_AndConsumesWithManualAck()
    {
        using var broker = new BrokerFixture();
        using var worker = broker.CreateWorker();

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await WaitUntilAsync(() => broker.Consume != null);
            Assert.Equal(
            [
                $"declare:{QueueNames.EmailDeadLetter}:durable=True:exclusive=False:autoDelete=False",
                $"declare:{QueueNames.Email}:durable=True:exclusive=False:autoDelete=False",
                $"declare:{EmailConsumerWorker.RetryQueueName(1)}:durable=True:exclusive=False:autoDelete=False",
                $"declare:{EmailConsumerWorker.RetryQueueName(2)}:durable=True:exclusive=False:autoDelete=False",
                $"declare:{EmailConsumerWorker.RetryQueueName(3)}:durable=True:exclusive=False:autoDelete=False",
                "qos",
                "consume",
            ], broker.Calls);
            Assert.Equal(QueueNames.CreateEmailQueueArguments().Keys.Order(), broker.EmailQueueArguments?.Keys.Order());
            foreach (var pair in QueueNames.CreateEmailQueueArguments())
                Assert.Equal(pair.Value, broker.EmailQueueArguments![pair.Key]);
            Assert.Equal((0u, (ushort)10, false), broker.Qos);
            Assert.Equal((QueueNames.Email, false), broker.Consume);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>
    /// The consumer's channel tracks publisher confirms, so the retry copy it publishes is confirmed by the broker before
    /// the original delivery is acked: a copy the broker refuses throws, the original stays unacked, and the mail is
    /// redelivered instead of being acked away with no copy left (the same guarantee the dead-letter re-drive relies on).
    /// </summary>
    [Fact]
    public async Task StartAsync_OpensItsChannelWithPublisherConfirms()
    {
        using var broker = new BrokerFixture();
        using var worker = broker.CreateWorker();

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await WaitUntilAsync(() => broker.Consume != null);
            Assert.True(broker.ChannelOptions?.PublisherConfirmationsEnabled);
            Assert.True(broker.ChannelOptions?.PublisherConfirmationTrackingEnabled);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>The broker often is not reachable the instant the worker starts: the initial connect is retried instead of faulting the host.</summary>
    [Fact]
    public async Task StartAsync_RetriesTheInitialConnection_UntilTheBrokerIsReachable()
    {
        using var broker = new BrokerFixture();
        int attempts = 0;
        broker.Factory.OnCreateConnectionAsync = _ => ++attempts < 3
            ? Task.FromException<IConnection>(new InvalidOperationException("broker not ready"))
            : Task.FromResult<IConnection>(broker.Connection);
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = broker.CreateWorker(logger);
        var started = System.Diagnostics.Stopwatch.StartNew();

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await WaitUntilAsync(() => broker.Consume != null);
            Assert.Equal(3, attempts);
            Assert.Equal((QueueNames.Email, false), broker.Consume);
            // Each failed attempt is logged with the wait before the next, and the wait doubles (10 ms, then 20 ms) — both
            // waits are actually taken before the third attempt connects.
            Assert.Equal(["0.01", "0.02"], logger.Collector.GetSnapshot()
                .Where(r => r.Level == LogLevel.Warning && r.Message.StartsWith("Could not connect to RabbitMQ; retrying in ", StringComparison.Ordinal))
                .Select(r => r.Message["Could not connect to RabbitMQ; retrying in ".Length..^1]));
            Assert.True(started.Elapsed >= TimeSpan.FromMilliseconds(30), $"connected after {started.Elapsed}");
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>A failure part-way through the topology setup tears down the half-opened channel and connection before the next attempt (no leak).</summary>
    [Fact]
    public async Task StartAsync_DisposesTheHalfOpenedChannelAndConnection_WhenTopologySetupFailsThenRetries()
    {
        using var broker = new BrokerFixture();
        using var failingChannel = new FakeChannel
        {
            OnQueueDeclareAsync = (_, _, _, _, _, _, _, _) => Task.FromException<QueueDeclareOk>(new InvalidOperationException("declare failed")),
        };
        using var failingConnection = new FakeConnection();
        var failingChannels = new Queue<IChannel>([failingChannel]);
        failingConnection.OnCreateChannelAsync = (_, _) => Task.FromResult(failingChannels.Dequeue());
        var connections = new Queue<IConnection>([failingConnection, broker.Connection]);
        broker.Factory.OnCreateConnectionAsync = _ => Task.FromResult(connections.Dequeue());
        using var worker = broker.CreateWorker();

        await worker.StartAsync(TestContext.Current.CancellationToken);
        try
        {
            await WaitUntilAsync(() => broker.Consume != null);
            Assert.Equal(1, failingChannel.DisposeAsyncCallCount);
            Assert.Equal(1, failingConnection.DisposeAsyncCallCount);
            Assert.Equal((QueueNames.Email, false), broker.Consume);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    /// <summary>Shutdown requested mid-connect closes what was already opened and stops (it neither retries nor leaks the connection).</summary>
    [Fact]
    public async Task StartAsync_DisposesWhatWasOpenedAndStops_WhenShutdownIsRequestedMidConnect()
    {
        using var broker = new BrokerFixture();
        using var cts = new CancellationTokenSource();
        broker.Channel.OnQueueDeclareAsync = (_, _, _, _, _, _, _, _) => CancelThenThrowAsync();
        using var worker = broker.CreateWorker();

        await worker.StartAsync(cts.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.ExecuteTask!);

        Assert.Equal(1, broker.Channel.DisposeAsyncCallCount);
        Assert.Equal(1, broker.Connection.DisposeAsyncCallCount);
        Assert.Single(broker.Factory.CreateConnectionAsyncCalls);
        return;

        // The topology setup is where shutdown arrives: the host's token is cancelled, and the call fails as cancelled.
        async Task<QueueDeclareOk> CancelThenThrowAsync()
        {
            await cts.CancelAsync();
            throw new OperationCanceledException(cts.Token);
        }
    }

    /// <summary>Stopping cancels the consumer first (no new deliveries), then releases the channel and connection.</summary>
    [Fact]
    public async Task StopAsync_CancelsTheConsumer_ThenDisposesTheChannelAndConnection()
    {
        using var broker = new BrokerFixture();
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = broker.CreateWorker(logger);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilConsumingAsync(logger);

        await worker.StopAsync(CancellationToken.None);

        Assert.Equal([("consumer-tag-1", false)], broker.Channel.BasicCancelAsyncCalls.Select(c => (c.ConsumerTag, c.NoWait)));
        Assert.Equal(1, broker.Channel.DisposeAsyncCallCount);
        Assert.Equal(1, broker.Connection.DisposeAsyncCallCount);
    }

    /// <summary>A failure cancelling the consumer during shutdown is logged, not thrown: the channel and connection are still released.</summary>
    [Fact]
    public async Task StopAsync_StillDisposesEverything_WhenCancellingTheConsumerFails()
    {
        using var broker = new BrokerFixture();
        broker.Channel.OnBasicCancelAsync = (_, _, _) => Task.FromException(new InvalidOperationException("channel already closed"));
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = broker.CreateWorker(logger);
        await worker.StartAsync(TestContext.Current.CancellationToken);
        await WaitUntilConsumingAsync(logger);

        await worker.StopAsync(CancellationToken.None);

        Assert.NotNull(SingleLog(logger, LogLevel.Warning, "Failed to cancel RabbitMQ consumer 'consumer-tag-1' during shutdown").Exception);
        Assert.Equal(1, broker.Channel.DisposeAsyncCallCount);
        Assert.Equal(1, broker.Connection.DisposeAsyncCallCount);
    }

    // -- Retry-count header parsing ---------------------------------------------------------------

    /// <summary>A delivery on the e-mail queue whose retry-count header holds <paramref name="headerValue"/> as-is.</summary>
    private static BasicDeliverEventArgs MakeDeliverArgsWithRawRetryHeader(byte[] body, object? headerValue, ulong deliveryTag) =>
        new("consumer-tag", deliveryTag, false, exchange: "", routingKey: QueueNames.Email,
            new BasicProperties { Headers = new Dictionary<string, object?> { [EmailConsumerWorker.RetryCountHeader] = headerValue } },
            body, CancellationToken.None);

    /// <summary>
    /// The retry counter survives however the broker hands the header back (an int we set, a long, or a
    /// UTF-8 byte string): a value already at the limit dead-letters instead of retrying forever.
    /// </summary>
    [Theory]
    [MemberData(nameof(ExhaustedRetryHeaders))]
    public async Task OnMessageReceivedAsync_DeadLettersInsteadOfRetrying_WhenTheRetryHeaderIsAtTheLimit(object headerValue)
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        using var worker = CreateSut(handler, out var channel);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgsWithRawRetryHeader(JsonSerializer.SerializeToUtf8Bytes(_message), headerValue, deliveryTag: 31));

        Assert.Equal([(31UL, false, false)], channel.BasicNackAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple, c.Requeue)));
        Assert.Empty(channel.BasicPublishAsyncCalls);
    }

    /// <summary>Header values that were exhausted, in each representation the client may deliver.</summary>
    public static TheoryData<object> ExhaustedRetryHeaders =>
    [

        EmailConsumerWorker.MaxRetries,
        (long)EmailConsumerWorker.MaxRetries,
        Encoding.UTF8.GetBytes(EmailConsumerWorker.MaxRetries.ToString(CultureInfo.InvariantCulture))
    ];

    /// <summary>A garbage or non-numeric header counts as "first attempt": the message still gets its retry.</summary>
    [Theory]
    // The rows are raw header values of different types (string, byte[], double) held as object, which xUnit
    // cannot serialize for discovery-time enumeration; the rows are enumerated when the theory runs instead.
    [MemberData(nameof(UnusableRetryHeaders), DisableDiscoveryEnumeration = true)]
    public async Task OnMessageReceivedAsync_TreatsAnUnusableRetryHeaderAsAFirstAttempt(object headerValue)
    {
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) => Task.FromException(new InvalidOperationException("SMTP down"));
        using var worker = CreateSut(handler, out var channel);

        await worker.OnMessageReceivedAsync(this, MakeDeliverArgsWithRawRetryHeader(JsonSerializer.SerializeToUtf8Bytes(_message), headerValue, deliveryTag: 32));

        Assert.Equal([(32UL, false)], channel.BasicAckAsyncCalls.Select(c => (c.DeliveryTag, c.Multiple)));
        Assert.Empty(channel.BasicNackAsyncCalls);
    }

    /// <summary>Header values that must not count as an exhausted retry budget.</summary>
    public static TheoryData<object> UnusableRetryHeaders =>
    [

        "not-a-number",
        "abc"u8.ToArray(),
        3.5
    ];

    /// <summary>
    /// Send that ignores cancellation is still running when the unwind window ends: the worker moves on
    /// to closing the channel regardless. That message will be redelivered, so it is logged as a warning
    /// (the drain-deadline warning before it only says cancellation was requested).
    /// </summary>
    [Fact]
    public async Task StopAsync_LogsAWarning_WhenASendIgnoresCancellationPastTheUnwindWindow()
    {
        var handlerStarted = new TaskCompletionSource();
        var release = new TaskCompletionSource();
        var handler = new FakeEmailMessageHandler();
        handler.OnHandleAsync = (_, _) =>
        {
            handlerStarted.SetResult();
            return release.Task; // ignores the token on purpose
        };
        var logger = new FakeLogger<EmailConsumerWorker>();
        using var worker = CreateSut(handler, out var channel, logger);
        worker.DrainTimeout = TimeSpan.FromMilliseconds(50);
        worker.CancelUnwindTimeout = TimeSpan.FromMilliseconds(100);
        byte[] body = JsonSerializer.SerializeToUtf8Bytes(_message);
        Task received = worker.OnMessageReceivedAsync(this, MakeDeliverArgs(body, deliveryTag: 31, redelivered: false));
        await handlerStarted.Task;

        await worker.StopAsync(CancellationToken.None);

        FakeLogRecord warning = Assert.Single(logger.Collector.GetSnapshot(),
            r => r.Level == LogLevel.Warning && r.Message.Contains("did not stop within the unwind window", StringComparison.Ordinal));
        Assert.Contains("1", warning.Message, StringComparison.Ordinal); // one send still in flight
        release.SetResult();
        await received;
    }
}
