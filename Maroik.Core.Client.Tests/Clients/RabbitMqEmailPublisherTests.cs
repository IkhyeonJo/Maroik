using Maroik.Core.Client.Clients;
using Maroik.Core.Client.Tests.Fixtures;
using Maroik.Core.Contract.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;

namespace Maroik.Core.Client.Tests.Clients;

/// <summary>
/// Unit tests for <see cref="RabbitMqEmailPublisher"/>.
/// Verifies lazy connection creation, connection and channel reuse while open, reconnection once
/// the cached connection drops, and the shape of the published AMQP message.
/// </summary>
public class RabbitMqEmailPublisherTests
{
    /// <summary>Fake <c>IConnectionFactory</c> injected into the system under test.</summary>
    private readonly FakeConnectionFactory _factory = new();
    /// <summary>The message every test publishes.</summary>
    private static readonly SendEmailMessage _message = new("to@test.com", "Subject", "Body", "corr-1");

    /// <summary>An open channel that accepts queue declarations and publishes.</summary>
    private static FakeChannel MakeChannel() => new() { IsOpen = true };

    /// <summary>A connection reporting <paramref name="isOpen"/> that hands out <paramref name="channel"/>.</summary>
    private static FakeConnection MakeConnection(IChannel channel, bool isOpen) =>
        new() { IsOpen = isOpen, OnCreateChannelAsync = (_, _) => Task.FromResult(channel) };

    /// <summary>Makes <see cref="_factory"/> hand out <paramref name="connections"/> one per call, in order.</summary>
    private void FactoryReturns(params IConnection[] connections)
    {
        var queue = new Queue<IConnection>(connections);
        _factory.OnCreateConnectionAsync = _ => Task.FromResult(queue.Dequeue());
    }

    // A short grace window so tests exercising the "connection dropped" path don't pay a real
    // multi-second sleep (the production default, sourced from ConnectionFactory.NetworkRecoveryInterval,
    // is 5s — see RabbitMqEmailPublisher's constructor).
    private static readonly TimeSpan _testRecoveryGraceWindow = TimeSpan.FromMilliseconds(20);

    /// <summary>Builds the publisher under test over the fake factory with a short reconnect grace window.</summary>
    private RabbitMqEmailPublisher CreateSut() =>
        new(_factory, NullLogger<RabbitMqEmailPublisher>.Instance, _testRecoveryGraceWindow);

    /// <summary>Publish async no cached connection opens connection and publishes.</summary>
    [Fact]
    public async Task PublishAsync_NoCachedConnection_OpensConnectionAndPublishes()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);

        await using var sut = CreateSut();
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        Assert.Single(_factory.CreateConnectionAsyncCalls);
        var publish = Assert.Single(channel.BasicPublishAsyncCalls);
        Assert.Equal(("", QueueNames.Email, false), (publish.Exchange, publish.RoutingKey, publish.Mandatory));
        Assert.True(Assert.IsType<BasicProperties>(publish.BasicProperties).Persistent);
    }

    /// <summary>
    /// Regression test: without publisher confirmations enabled, BasicPublishAsync returns as
    /// soon as the message is locally buffered, so a dropped connection right after the write
    /// silently loses the message even though PublishAsync reported success. Verifies the channel
    /// is created with confirmations (and tracking) enabled so BasicPublishAsync actually waits
    /// for and validates the broker's ack.
    /// </summary>
    [Fact]
    public async Task PublishAsync_CreatesChannel_WithPublisherConfirmationsEnabled()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);

        await using var sut = CreateSut();
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        var options = Assert.Single(connection.CreateChannelAsyncCalls).Options;
        Assert.NotNull(options);
        Assert.True(options.PublisherConfirmationsEnabled && options.PublisherConfirmationTrackingEnabled);
    }

    /// <summary>Publish async publishes message body as JSON.</summary>
    [Fact]
    public async Task PublishAsync_PublishesMessageBodyAsJson()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);

        await using var sut = CreateSut();
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SendEmailMessage>(Assert.Single(channel.BasicPublishAsyncCalls).Body);
        Assert.Equal(_message, deserialized);
    }

    /// <summary>Publish async connection stays open reuses connection across calls.</summary>
    [Fact]
    public async Task PublishAsync_ConnectionStaysOpen_ReusesConnectionAcrossCalls()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        await using var sut = CreateSut();

        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        Assert.Single(_factory.CreateConnectionAsyncCalls);
        Assert.Equal(2, channel.BasicPublishAsyncCalls.Count);
    }

    /// <summary>
    /// Publish async, channel stays open: the channel (and its one-time queue declare) is reused
    /// across calls rather than a fresh channel being opened and the queue redeclared per publish.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ChannelStaysOpen_ReusesChannelAndDeclaresQueueOnce()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        await using var sut = CreateSut();

        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        Assert.Single(connection.CreateChannelAsyncCalls);
        var declare = Assert.Single(channel.QueueDeclareAsyncCalls);
        Assert.Equal((QueueNames.Email, true, false, false), (declare.Queue, declare.Durable, declare.Exclusive, declare.AutoDelete));
        Assert.Equal(2, channel.BasicPublishAsyncCalls.Count);
    }

    /// <summary>Publish async cached connection closed disposes it and opens a new one.</summary>
    [Fact]
    public async Task PublishAsync_CachedConnectionClosed_DisposesItAndOpensANewOne()
    {
        using var channel1 = MakeChannel();
        using var deadConnection = MakeConnection(channel1, isOpen: false);
        using var channel2 = MakeChannel();
        using var freshConnection = MakeConnection(channel2, isOpen: true);
        FactoryReturns(deadConnection, freshConnection);
        await using var sut = CreateSut();

        await sut.PublishAsync(_message, TestContext.Current.CancellationToken); // caches deadConnection (its own open state is irrelevant on creation)
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken); // now sees deadConnection.IsOpen == false, must reconnect

        Assert.Equal(2, _factory.CreateConnectionAsyncCalls.Count);
        Assert.Equal(1, deadConnection.DisposeAsyncCallCount);
        Assert.Single(channel2.BasicPublishAsyncCalls);
    }

    /// <summary>
    /// Verifies that when the cached connection is seen as closed but flips back to open by the
    /// time the short recovery grace window elapses (AutomaticRecoveryEnabled's own reconnection
    /// loop finishing first), GetChannelAsync reuses the same connection instead of disposing it
    /// and racing that recovery with a brand-new one.
    /// </summary>
    [Fact]
    public async Task PublishAsync_ConnectionRecoversDuringGraceWindow_ReusesConnection_WithoutReconnecting()
    {
        using var channel2 = MakeChannel();
        using var channel1 = MakeChannel();
        var channels = new Queue<IChannel>([channel1, channel2]);
        // IsOpen is never read while _connection is still null (pattern-matching a null reference
        // short-circuits without touching the member), so the first real read happens on the
        // second publish's cached-fast-path check below.
        var isOpenReads = new Queue<bool>([
            false, // second publish: cached-fast-path check — looks dropped
            false, // entering the "not open" branch
            true, // re-checked after the grace-window delay: recovered
            true, // final re-check before falling through to a fresh connection
        ]);
        using var connection = new FakeConnection
        {
            OnIsOpen = () => isOpenReads.Count > 0 && isOpenReads.Dequeue(),
            OnCreateChannelAsync = (_, _) => Task.FromResult(channels.Dequeue()),
        };
        FactoryReturns(connection);
        await using var sut = CreateSut();

        await sut.PublishAsync(_message, TestContext.Current.CancellationToken); // caches the connection
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken); // sees it "closed", waits, sees it recovered

        Assert.Single(_factory.CreateConnectionAsyncCalls);
        Assert.Equal(0, connection.DisposeAsyncCallCount);
        Assert.Single(channel2.BasicPublishAsyncCalls);
    }

    /// <summary>
    /// Regression: while the broker is down, a failed connect attempt makes the publishes that follow
    /// within the grace window fail fast — they do not each queue behind the lock and pay another
    /// (multi-second) connect attempt, which stacked up request latency during an outage.
    /// </summary>
    [Fact]
    public async Task PublishAsync_AfterAFailedConnect_FailsFast_WithoutAnotherConnectAttempt()
    {
        _factory.OnCreateConnectionAsync = _ => Task.FromException<IConnection>(new InvalidOperationException("broker down"));
        await using var sut = new RabbitMqEmailPublisher(_factory, NullLogger<RabbitMqEmailPublisher>.Instance, TimeSpan.FromSeconds(30));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PublishAsync(_message, TestContext.Current.CancellationToken));
        var fastFail = await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PublishAsync(_message, TestContext.Current.CancellationToken));

        Assert.Contains("unavailable", fastFail.Message, StringComparison.Ordinal);
        Assert.Single(_factory.CreateConnectionAsyncCalls);
    }

    /// <summary>Once the grace window has passed the publisher tries to connect again, and a success clears the failure.</summary>
    [Fact]
    public async Task PublishAsync_ReconnectsAfterTheGraceWindow_AndPublishes()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        var afterTheOutage = new Queue<IConnection>([connection]);
        var attempts = 0;
        _factory.OnCreateConnectionAsync = _ => ++attempts == 1
            ? Task.FromException<IConnection>(new InvalidOperationException("broker down"))
            : Task.FromResult(afterTheOutage.Dequeue());
        await using var sut = new RabbitMqEmailPublisher(_factory, NullLogger<RabbitMqEmailPublisher>.Instance, TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PublishAsync(_message, TestContext.Current.CancellationToken));
        await Task.Delay(150, TestContext.Current.CancellationToken);
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        Assert.Equal(2, _factory.CreateConnectionAsyncCalls.Count);
        Assert.Single(channel.BasicPublishAsyncCalls);
    }

    /// <summary>
    /// Regression: the recovery-grace-window fix must actually track whatever
    /// <c>NetworkRecoveryInterval</c> a real <see cref="ConnectionFactory"/> is configured with
    /// (defaulting to a fixed constant here previously either stalled every reconnect on a mismatch
    /// or, before that, was too short to ever observe a real recovery). Reflection reaches the
    /// private field directly since the grace window is otherwise only observable by actually
    /// waiting it out in real time.
    /// </summary>
    [Fact]
    public async Task Constructor_SourcesRecoveryGraceWindow_FromConnectionFactoryNetworkRecoveryInterval()
    {
        var realFactory = new ConnectionFactory { NetworkRecoveryInterval = TimeSpan.FromSeconds(42) };

        await using var sut = new RabbitMqEmailPublisher(realFactory, NullLogger<RabbitMqEmailPublisher>.Instance);

        var field = typeof(RabbitMqEmailPublisher).GetField(
            "_connectionRecoveryGraceWindow", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.Equal(TimeSpan.FromSeconds(42), (TimeSpan)field!.GetValue(sut)!);
    }

    /// <summary>
    /// A publish wedged on the broker keeps the lock past the dispose timeout: disposal is skipped and logged (the channel and
    /// connection stay with the call still using them), and a later dispose, once the publish has finished, cleans up.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_SkipsAndLogs_WhileAPublishIsWedged_AndCanBeRepeatedAfterwards()
    {
        var release = new TaskCompletionSource();
        var entered = new TaskCompletionSource();
        using var channel = MakeChannel();
        channel.OnBasicPublishAsync = (_, _, _, _, _, _) => { entered.TrySetResult(); return new ValueTask(release.Task); };
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        var logger = new Microsoft.Extensions.Logging.Testing.FakeLogger<RabbitMqEmailPublisher>();
        var sut = new RabbitMqEmailPublisher(_factory, logger, _testRecoveryGraceWindow) { DisposeLockTimeout = TimeSpan.FromMilliseconds(100) };
        Task publish = sut.PublishAsync(_message, TestContext.Current.CancellationToken);
        await entered.Task;

        await sut.DisposeAsync();

        Assert.Equal(0, connection.DisposeAsyncCallCount);
        Assert.Contains(logger.Collector.GetSnapshot(), r =>
            r.Level == Microsoft.Extensions.Logging.LogLevel.Warning && r.Message.Contains("waiting for an in-flight publish to finish", StringComparison.Ordinal));

        release.SetResult();
        await publish;
        await sut.DisposeAsync();

        Assert.Equal(1, connection.DisposeAsyncCallCount);
    }

    /// <summary>Dispose async with open connection disposes connection.</summary>
    [Fact]
    public async Task DisposeAsync_WithOpenConnection_DisposesConnection()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        await using var sut = CreateSut();
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        await sut.DisposeAsync();

        Assert.Equal(1, connection.DisposeAsyncCallCount);
    }

    /// <summary>
    /// Disposing twice is harmless, as <see cref="IAsyncDisposable"/> requires: the second call does nothing (it neither
    /// throws on the already-disposed lock nor disposes the connection again).
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow_AndDisposesTheConnectionOnce()
    {
        using var channel = MakeChannel();
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        await using var sut = CreateSut();
        await sut.PublishAsync(_message, TestContext.Current.CancellationToken);

        await sut.DisposeAsync();
        Exception? second = await Record.ExceptionAsync(async () => await sut.DisposeAsync());

        Assert.Null(second);
        Assert.Equal(1, connection.DisposeAsyncCallCount);
    }

    /// <summary>
    /// Two disposes that arrive together while a publish still holds the lock: one does the disposal, the other returns
    /// without waiting its turn — neither throws (on the already-disposed lock) and the channel is disposed once.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_CalledConcurrently_DisposesOnce_AndNeitherCallThrows()
    {
        var unblock = new TaskCompletionSource();
        using var channel = MakeChannel();
        channel.OnBasicPublishAsync = (_, _, _, _, _, _) => new ValueTask(unblock.Task);
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        var sut = CreateSut();
        Task publish = sut.PublishAsync(_message, CancellationToken.None);

        Task first = sut.DisposeAsync().AsTask();
        Task second = sut.DisposeAsync().AsTask();
        unblock.SetResult();
        await publish;
        Exception? thrown = await Record.ExceptionAsync(() => Task.WhenAll(first, second));

        Assert.Null(thrown);
        Assert.Equal(1, channel.DisposeAsyncCallCount);
        Assert.Equal(1, connection.DisposeAsyncCallCount);
    }

    /// <summary>
    /// A dispose that timed out behind a stuck publish disposed nothing, so a later dispose — once the publish has
    /// finished — still disposes the channel and the connection.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_AfterATimedOutDispose_StillDisposesOnceThePublishHasFinished()
    {
        var unblock = new TaskCompletionSource();
        using var channel = MakeChannel();
        channel.OnBasicPublishAsync = (_, _, _, _, _, _) => new ValueTask(unblock.Task);
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        await using var sut = CreateSut();
        Task stuckPublish = sut.PublishAsync(_message, CancellationToken.None);

        await sut.DisposeAsync(); // times out (after its fixed 5 s bound): the publish still holds the lock
        unblock.SetResult();
        await stuckPublish;
        await sut.DisposeAsync();

        Assert.Equal(1, channel.DisposeAsyncCallCount);
        Assert.Equal(1, connection.DisposeAsyncCallCount);
    }

    /// <summary>Dispose async never published does not throw.</summary>
    [Fact]
    public async Task DisposeAsync_NeverPublished_DoesNotThrow()
    {
        await using var sut = CreateSut();

        await sut.DisposeAsync();

        Assert.Empty(_factory.CreateConnectionAsyncCalls);
    }

    /// <summary>
    /// Regression test: DisposeAsync must not hang forever if a PublishAsync call is stuck holding
    /// the publishing lock (e.g. a broker that never confirms, with the caller having passed
    /// CancellationToken.None). Verifies dispose completes within a bounded time instead of
    /// deadlocking application shutdown, and -- since the stuck publishing still owns them -- that it
    /// does NOT dispose the channel/connection out from under that still-running call.
    /// </summary>
    [Fact]
    public async Task DisposeAsync_DoesNotHangForever_WhenPublishIsStuckHoldingTheLock()
    {
        var neverCompletes = new TaskCompletionSource();
        using var channel = MakeChannel();
        channel.OnBasicPublishAsync = (_, _, _, _, _, _) => new ValueTask(neverCompletes.Task);
        using var connection = MakeConnection(channel, isOpen: true);
        FactoryReturns(connection);
        await using var sut = CreateSut();

        Task stuckPublish = sut.PublishAsync(_message, CancellationToken.None);

        Task disposeTask = sut.DisposeAsync().AsTask();
        Task completedFirst = await Task.WhenAny(disposeTask, Task.Delay(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));

        Assert.Same(disposeTask, completedFirst);
        Assert.Equal(0, channel.DisposeAsyncCallCount);
        Assert.Equal(0, connection.DisposeAsyncCallCount);

        neverCompletes.SetResult(); // unblock the stuck publishing so it doesn't leak past this test
        await stuckPublish;
    }
}
