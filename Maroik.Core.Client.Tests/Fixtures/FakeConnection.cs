using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Maroik.Core.Client.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IConnection"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeConnection : IConnection
{
    /// <summary>The value <see cref="IConnection.ChannelMax"/> answers; settable by a test.</summary>
    public ushort ChannelMax { get; set; }

    /// <summary>The value <see cref="IConnection.ClientProperties"/> answers; settable by a test.</summary>
    public IDictionary<string, object?> ClientProperties { get; set; } = new Dictionary<string, object?>();

    /// <summary>The value <see cref="IConnection.CloseReason"/> answers; settable by a test.</summary>
    public ShutdownEventArgs? CloseReason { get; set; }

    /// <summary>The value <see cref="IConnection.Endpoint"/> answers; settable by a test.</summary>
    public AmqpTcpEndpoint Endpoint { get; set; } = default!;

    /// <summary>The value <see cref="IConnection.FrameMax"/> answers; settable by a test.</summary>
    public uint FrameMax { get; set; }

    /// <summary>The value <see cref="IConnection.Heartbeat"/> answers; settable by a test.</summary>
    public TimeSpan Heartbeat { get; set; }

    /// <summary>The value <see cref="IConnection.IsOpen"/> answers (unless <see cref="OnIsOpen"/> is set); settable by a test.</summary>
    public bool IsOpen
    {
        get => OnIsOpen is { } on ? on() : _isOpen;
        set => _isOpen = value;
    }

    /// <summary>What a read of <see cref="IsOpen"/> answers, for a test that needs the state to change between reads.</summary>
    public Func<bool>? OnIsOpen { get; set; }

    /// <summary>The value <see cref="IsOpen"/> answers when <see cref="OnIsOpen"/> is not set.</summary>
    private bool _isOpen;

    /// <summary>The value <see cref="IConnection.Protocol"/> answers; settable by a test.</summary>
    public IProtocol Protocol { get; set; } = default!;

    /// <summary>The value <see cref="IConnection.ServerProperties"/> answers; settable by a test.</summary>
    public IDictionary<string, object?>? ServerProperties { get; set; }

    /// <summary>The value <see cref="IConnection.ShutdownReport"/> answers; settable by a test.</summary>
    public IEnumerable<ShutdownReportEntry> ShutdownReport { get; set; } = [];

    /// <summary>The value <see cref="IConnection.ClientProvidedName"/> answers; settable by a test.</summary>
    public string? ClientProvidedName { get; set; }

    /// <summary>The value <see cref="INetworkConnection.LocalPort"/> answers; settable by a test.</summary>
    public int LocalPort { get; set; }

    /// <summary>The value <see cref="INetworkConnection.RemotePort"/> answers; settable by a test.</summary>
    public int RemotePort { get; set; }

    /// <summary>Subscribers to <c>CallbackExceptionAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<CallbackExceptionEventArgs>> CallbackExceptionAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<CallbackExceptionEventArgs> CallbackExceptionAsync { add => CallbackExceptionAsyncHandlers.Add(value); remove => CallbackExceptionAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConnectionShutdownAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<ShutdownEventArgs>> ConnectionShutdownAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<ShutdownEventArgs> ConnectionShutdownAsync { add => ConnectionShutdownAsyncHandlers.Add(value); remove => ConnectionShutdownAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>RecoverySucceededAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<AsyncEventArgs>> RecoverySucceededAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<AsyncEventArgs> RecoverySucceededAsync { add => RecoverySucceededAsyncHandlers.Add(value); remove => RecoverySucceededAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConnectionRecoveryErrorAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<ConnectionRecoveryErrorEventArgs>> ConnectionRecoveryErrorAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<ConnectionRecoveryErrorEventArgs> ConnectionRecoveryErrorAsync { add => ConnectionRecoveryErrorAsyncHandlers.Add(value); remove => ConnectionRecoveryErrorAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConsumerTagChangeAfterRecoveryAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<ConsumerTagChangedAfterRecoveryEventArgs>> ConsumerTagChangeAfterRecoveryAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<ConsumerTagChangedAfterRecoveryEventArgs> ConsumerTagChangeAfterRecoveryAsync { add => ConsumerTagChangeAfterRecoveryAsyncHandlers.Add(value); remove => ConsumerTagChangeAfterRecoveryAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>QueueNameChangedAfterRecoveryAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<QueueNameChangedAfterRecoveryEventArgs>> QueueNameChangedAfterRecoveryAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<QueueNameChangedAfterRecoveryEventArgs> QueueNameChangedAfterRecoveryAsync { add => QueueNameChangedAfterRecoveryAsyncHandlers.Add(value); remove => QueueNameChangedAfterRecoveryAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>RecoveringConsumerAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<RecoveringConsumerEventArgs>> RecoveringConsumerAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<RecoveringConsumerEventArgs> RecoveringConsumerAsync { add => RecoveringConsumerAsyncHandlers.Add(value); remove => RecoveringConsumerAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConnectionBlockedAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<ConnectionBlockedEventArgs>> ConnectionBlockedAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<ConnectionBlockedEventArgs> ConnectionBlockedAsync { add => ConnectionBlockedAsyncHandlers.Add(value); remove => ConnectionBlockedAsyncHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConnectionUnblockedAsync</c>, kept so a test can raise it.</summary>
    public List<AsyncEventHandler<AsyncEventArgs>> ConnectionUnblockedAsyncHandlers { get; } = [];

    /// <inheritdoc/>
    public event AsyncEventHandler<AsyncEventArgs> ConnectionUnblockedAsync { add => ConnectionUnblockedAsyncHandlers.Add(value); remove => ConnectionUnblockedAsyncHandlers.Remove(value); }

    /// <summary>What <c>UpdateSecretAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task>? OnUpdateSecretAsync { get; set; }

    /// <summary>The arguments of every <c>UpdateSecretAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string NewSecret, string Reason, CancellationToken CancellationToken)> UpdateSecretAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task UpdateSecretAsync(string newSecret, string reason, CancellationToken cancellationToken)
    {
        lock (UpdateSecretAsyncCalls) UpdateSecretAsyncCalls.Add((newSecret, reason, cancellationToken));
        if (OnUpdateSecretAsync is { } on)
        {
            return on(newSecret, reason, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>CloseAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ushort, string, TimeSpan, bool, CancellationToken, Task>? OnCloseAsync { get; set; }

    /// <summary>The arguments of every <c>CloseAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(ushort ReasonCode, string ReasonText, TimeSpan Timeout, bool Abort, CancellationToken CancellationToken)> CloseAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task CloseAsync(ushort reasonCode, string reasonText, TimeSpan timeout, bool abort, CancellationToken cancellationToken)
    {
        lock (CloseAsyncCalls) CloseAsyncCalls.Add((reasonCode, reasonText, timeout, abort, cancellationToken));
        if (OnCloseAsync is { } on)
        {
            return on(reasonCode, reasonText, timeout, abort, cancellationToken);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>CreateChannelAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CreateChannelOptions?, CancellationToken, Task<IChannel>>? OnCreateChannelAsync { get; set; }

    /// <summary>The arguments of every <c>CreateChannelAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(CreateChannelOptions? Options, CancellationToken CancellationToken)> CreateChannelAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<IChannel> CreateChannelAsync(CreateChannelOptions? options, CancellationToken cancellationToken)
    {
        lock (CreateChannelAsyncCalls) CreateChannelAsyncCalls.Add((options, cancellationToken));
        if (OnCreateChannelAsync is { } on)
        {
            return on(options, cancellationToken);
        }

        return Task.FromResult<IChannel>(default!);
    }

    /// <summary>What <c>DisposeAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ValueTask>? OnDisposeAsync { get; set; }

    /// <summary>How many times <c>DisposeAsync</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int DisposeAsyncCallCount => Volatile.Read(ref _disposeAsyncCallCount);

    /// <summary>Backs <see cref="DisposeAsyncCallCount"/>.</summary>
    private int _disposeAsyncCallCount;

    /// <inheritdoc/>
    public ValueTask DisposeAsync()
    {
        Interlocked.Increment(ref _disposeAsyncCallCount);
        if (OnDisposeAsync is { } on)
        {
            return on();
        }

        return ValueTask.CompletedTask;
    }

    /// <summary>What <c>Dispose</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action? OnDispose { get; set; }

    /// <summary>How many times <c>Dispose</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int DisposeCallCount => Volatile.Read(ref _disposeCallCount);

    /// <summary>Backs <see cref="DisposeCallCount"/>.</summary>
    private int _disposeCallCount;

    /// <inheritdoc/>
    public void Dispose()
    {
        Interlocked.Increment(ref _disposeCallCount);
        OnDispose?.Invoke();
    }
}
