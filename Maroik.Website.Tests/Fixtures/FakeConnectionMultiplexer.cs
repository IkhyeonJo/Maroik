using StackExchange.Redis;
using StackExchange.Redis.Maintenance;
using StackExchange.Redis.Profiling;
using System.Net;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IConnectionMultiplexer"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeConnectionMultiplexer : IConnectionMultiplexer
{
    /// <inheritdoc/>
    public override string ToString() => GetType().Name;

    /// <summary>The value <see cref="IConnectionMultiplexer.ClientName"/> answers; settable by a test.</summary>
    public string ClientName { get; set; } = default!;

    /// <summary>The value <see cref="IConnectionMultiplexer.Configuration"/> answers; settable by a test.</summary>
    public string Configuration { get; set; } = default!;

    /// <summary>The value <see cref="IConnectionMultiplexer.TimeoutMilliseconds"/> answers; settable by a test.</summary>
    public int TimeoutMilliseconds { get; set; }

    /// <summary>The value <see cref="IConnectionMultiplexer.OperationCount"/> answers; settable by a test.</summary>
    public long OperationCount { get; set; }

    /// <summary>The value <see cref="IConnectionMultiplexer.PreserveAsyncOrder"/> answers; settable by a test.</summary>
    public bool PreserveAsyncOrder { get; set; }

    /// <summary>The value <see cref="IConnectionMultiplexer.IsConnected"/> answers; settable by a test.</summary>
    public bool IsConnected { get; set; }

    /// <summary>The value <see cref="IConnectionMultiplexer.IsConnecting"/> answers; settable by a test.</summary>
    public bool IsConnecting { get; set; }

    /// <summary>The value <see cref="IConnectionMultiplexer.IncludeDetailInExceptions"/> answers; settable by a test.</summary>
    public bool IncludeDetailInExceptions { get; set; }

    /// <summary>The value <see cref="IConnectionMultiplexer.StormLogThreshold"/> answers; settable by a test.</summary>
    public int StormLogThreshold { get; set; }

    /// <summary>Subscribers to <c>ErrorMessage</c>, kept so a test can raise it.</summary>
    public List<EventHandler<RedisErrorEventArgs>> ErrorMessageHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<RedisErrorEventArgs> ErrorMessage { add => ErrorMessageHandlers.Add(value); remove => ErrorMessageHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConnectionFailed</c>, kept so a test can raise it.</summary>
    public List<EventHandler<ConnectionFailedEventArgs>> ConnectionFailedHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<ConnectionFailedEventArgs> ConnectionFailed { add => ConnectionFailedHandlers.Add(value); remove => ConnectionFailedHandlers.Remove(value); }

    /// <summary>Subscribers to <c>InternalError</c>, kept so a test can raise it.</summary>
    public List<EventHandler<InternalErrorEventArgs>> InternalErrorHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<InternalErrorEventArgs> InternalError { add => InternalErrorHandlers.Add(value); remove => InternalErrorHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConnectionRestored</c>, kept so a test can raise it.</summary>
    public List<EventHandler<ConnectionFailedEventArgs>> ConnectionRestoredHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<ConnectionFailedEventArgs> ConnectionRestored { add => ConnectionRestoredHandlers.Add(value); remove => ConnectionRestoredHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConfigurationChanged</c>, kept so a test can raise it.</summary>
    public List<EventHandler<EndPointEventArgs>> ConfigurationChangedHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<EndPointEventArgs> ConfigurationChanged { add => ConfigurationChangedHandlers.Add(value); remove => ConfigurationChangedHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ConfigurationChangedBroadcast</c>, kept so a test can raise it.</summary>
    public List<EventHandler<EndPointEventArgs>> ConfigurationChangedBroadcastHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<EndPointEventArgs> ConfigurationChangedBroadcast { add => ConfigurationChangedBroadcastHandlers.Add(value); remove => ConfigurationChangedBroadcastHandlers.Remove(value); }

    /// <summary>Subscribers to <c>ServerMaintenanceEvent</c>, kept so a test can raise it.</summary>
    public List<EventHandler<ServerMaintenanceEvent>> ServerMaintenanceEventHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<ServerMaintenanceEvent> ServerMaintenanceEvent { add => ServerMaintenanceEventHandlers.Add(value); remove => ServerMaintenanceEventHandlers.Remove(value); }

    /// <summary>Subscribers to <c>HashSlotMoved</c>, kept so a test can raise it.</summary>
    public List<EventHandler<HashSlotMovedEventArgs>> HashSlotMovedHandlers { get; } = [];

    /// <inheritdoc/>
    public event EventHandler<HashSlotMovedEventArgs> HashSlotMoved { add => HashSlotMovedHandlers.Add(value); remove => HashSlotMovedHandlers.Remove(value); }

    /// <summary>What <c>RegisterProfiler</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<Func<ProfilingSession?>>? OnRegisterProfiler { get; set; }

    /// <summary>The arguments of every <c>RegisterProfiler</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<Func<ProfilingSession?>> RegisterProfilerCalls { get; } = [];

    /// <inheritdoc/>
    public void RegisterProfiler(Func<ProfilingSession?> profilingSessionProvider)
    {
        lock (RegisterProfilerCalls) RegisterProfilerCalls.Add(profilingSessionProvider);
        OnRegisterProfiler?.Invoke(profilingSessionProvider);
    }

    /// <summary>What <c>GetCounters</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<ServerCounters>? OnGetCounters { get; set; }

    /// <summary>How many times <c>GetCounters</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int GetCountersCallCount => Volatile.Read(ref _getCountersCallCount);

    /// <summary>Backs <see cref="GetCountersCallCount"/>.</summary>
    private int _getCountersCallCount;

    /// <inheritdoc/>
    public ServerCounters GetCounters()
    {
        Interlocked.Increment(ref _getCountersCallCount);
        if (OnGetCounters is { } on)
        {
            return on();
        }

        return default!;
    }

    /// <summary>What <c>GetEndPoints</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<bool, EndPoint[]>? OnGetEndPoints { get; set; }

    /// <summary>The arguments of every <c>GetEndPoints</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<bool> GetEndPointsCalls { get; } = [];

    /// <inheritdoc/>
    public EndPoint[] GetEndPoints(bool configuredOnly)
    {
        lock (GetEndPointsCalls) GetEndPointsCalls.Add(configuredOnly);
        if (OnGetEndPoints is { } on)
        {
            return on(configuredOnly);
        }

        return [];
    }

    /// <summary>What <c>Wait</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<Task>? OnWait { get; set; }

    /// <summary>The arguments of every <c>Wait</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<Task> WaitCalls { get; } = [];

    /// <inheritdoc/>
    public void Wait(Task task)
    {
        lock (WaitCalls) WaitCalls.Add(task);
        OnWait?.Invoke(task);
    }

    /// <inheritdoc/>
    T IConnectionMultiplexer.Wait<T>(Task<T> task) => throw new NotSupportedException("Wait is not faked.");

    /// <summary>What <c>WaitAll</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<Task[]>? OnWaitAll { get; set; }

    /// <summary>The arguments of every <c>WaitAll</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<Task[]> WaitAllCalls { get; } = [];

    /// <inheritdoc/>
    public void WaitAll(Task[] tasks)
    {
        lock (WaitAllCalls) WaitAllCalls.Add(tasks);
        OnWaitAll?.Invoke(tasks);
    }

    /// <summary>What <c>HashSlot</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<RedisKey, int>? OnHashSlot { get; set; }

    /// <summary>The arguments of every <c>HashSlot</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<RedisKey> HashSlotCalls { get; } = [];

    /// <inheritdoc/>
    public int HashSlot(RedisKey key)
    {
        lock (HashSlotCalls) HashSlotCalls.Add(key);
        if (OnHashSlot is { } on)
        {
            return on(key);
        }

        return default;
    }

    /// <summary>What <c>GetSubscriber</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<object?, ISubscriber>? OnGetSubscriber { get; set; }

    /// <summary>The arguments of every <c>GetSubscriber</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<object?> GetSubscriberCalls { get; } = [];

    /// <inheritdoc/>
    public ISubscriber GetSubscriber(object? asyncState)
    {
        lock (GetSubscriberCalls) GetSubscriberCalls.Add(asyncState);
        if (OnGetSubscriber is { } on)
        {
            return on(asyncState);
        }

        return default!;
    }

    /// <summary>What <c>GetDatabase</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<int, object?, IDatabase>? OnGetDatabase { get; set; }

    /// <summary>The arguments of every <c>GetDatabase</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(int Db, object? AsyncState)> GetDatabaseCalls { get; } = [];

    /// <inheritdoc/>
    public IDatabase GetDatabase(int db, object? asyncState)
    {
        lock (GetDatabaseCalls) GetDatabaseCalls.Add((db, asyncState));
        if (OnGetDatabase is { } on)
        {
            return on(db, asyncState);
        }

        return default!;
    }

    /// <summary>What <c>GetServer</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, int, object?, IServer>? OnGetServerWithHostPort { get; set; }

    /// <summary>The arguments of every <c>GetServer</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string Host, int Port, object? AsyncState)> GetServerWithHostPortCalls { get; } = [];

    /// <inheritdoc/>
    public IServer GetServer(string host, int port, object? asyncState)
    {
        lock (GetServerWithHostPortCalls) GetServerWithHostPortCalls.Add((host, port, asyncState));
        if (OnGetServerWithHostPort is { } on)
        {
            return on(host, port, asyncState);
        }

        return default!;
    }

    /// <summary>What <c>GetServer</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, object?, IServer>? OnGetServer { get; set; }

    /// <summary>The arguments of every <c>GetServer</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string HostAndPort, object? AsyncState)> GetServerCalls { get; } = [];

    /// <inheritdoc/>
    public IServer GetServer(string hostAndPort, object? asyncState)
    {
        lock (GetServerCalls) GetServerCalls.Add((hostAndPort, asyncState));
        if (OnGetServer is { } on)
        {
            return on(hostAndPort, asyncState);
        }

        return default!;
    }

    /// <summary>What <c>GetServer</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IPAddress, int, IServer>? OnGetServerWithHostPortX { get; set; }

    /// <summary>The arguments of every <c>GetServer</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(IPAddress Host, int Port)> GetServerWithHostPortXCalls { get; } = [];

    /// <inheritdoc/>
    public IServer GetServer(IPAddress host, int port)
    {
        lock (GetServerWithHostPortXCalls) GetServerWithHostPortXCalls.Add((host, port));
        if (OnGetServerWithHostPortX is { } on)
        {
            return on(host, port);
        }

        return default!;
    }

    /// <summary>What <c>GetServer</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<EndPoint, object?, IServer>? OnGetServerWithEndpoint { get; set; }

    /// <summary>The arguments of every <c>GetServer</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(EndPoint Endpoint, object? AsyncState)> GetServerWithEndpointCalls { get; } = [];

    /// <inheritdoc/>
    public IServer GetServer(EndPoint endpoint, object? asyncState)
    {
        lock (GetServerWithEndpointCalls) GetServerWithEndpointCalls.Add((endpoint, asyncState));
        if (OnGetServerWithEndpoint is { } on)
        {
            return on(endpoint, asyncState);
        }

        return default!;
    }

    /// <summary>What <c>GetServers</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<IServer[]>? OnGetServers { get; set; }

    /// <summary>How many times <c>GetServers</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int GetServersCallCount => Volatile.Read(ref _getServersCallCount);

    /// <summary>Backs <see cref="GetServersCallCount"/>.</summary>
    private int _getServersCallCount;

    /// <inheritdoc/>
    public IServer[] GetServers()
    {
        Interlocked.Increment(ref _getServersCallCount);
        if (OnGetServers is { } on)
        {
            return on();
        }

        return [];
    }

    /// <summary>What <c>ConfigureAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<TextWriter?, Task<bool>>? OnConfigureAsync { get; set; }

    /// <summary>The arguments of every <c>ConfigureAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<TextWriter?> ConfigureAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<bool> ConfigureAsync(TextWriter? log)
    {
        lock (ConfigureAsyncCalls) ConfigureAsyncCalls.Add(log);
        if (OnConfigureAsync is { } on)
        {
            return on(log);
        }

        return Task.FromResult<bool>(default);
    }

    /// <summary>What <c>Configure</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<TextWriter?, bool>? OnConfigure { get; set; }

    /// <summary>The arguments of every <c>Configure</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<TextWriter?> ConfigureCalls { get; } = [];

    /// <inheritdoc/>
    public bool Configure(TextWriter? log)
    {
        lock (ConfigureCalls) ConfigureCalls.Add(log);
        if (OnConfigure is { } on)
        {
            return on(log);
        }

        return default;
    }

    /// <summary>What <c>GetStatus</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string>? OnGetStatus { get; set; }

    /// <summary>How many times <c>GetStatus</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int GetStatusCallCount => Volatile.Read(ref _getStatusCallCount);

    /// <summary>Backs <see cref="GetStatusCallCount"/>.</summary>
    private int _getStatusCallCount;

    /// <inheritdoc/>
    public string GetStatus()
    {
        Interlocked.Increment(ref _getStatusCallCount);
        if (OnGetStatus is { } on)
        {
            return on();
        }

        return default!;
    }

    /// <summary>What <c>GetStatus</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<TextWriter>? OnGetStatusWithLog { get; set; }

    /// <summary>The arguments of every <c>GetStatus</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<TextWriter> GetStatusWithLogCalls { get; } = [];

    /// <inheritdoc/>
    public void GetStatus(TextWriter log)
    {
        lock (GetStatusWithLogCalls) GetStatusWithLogCalls.Add(log);
        OnGetStatusWithLog?.Invoke(log);
    }

    /// <summary>What <c>Close</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<bool>? OnClose { get; set; }

    /// <summary>The arguments of every <c>Close</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<bool> CloseCalls { get; } = [];

    /// <inheritdoc/>
    public void Close(bool allowCommandsToComplete)
    {
        lock (CloseCalls) CloseCalls.Add(allowCommandsToComplete);
        OnClose?.Invoke(allowCommandsToComplete);
    }

    /// <summary>What <c>CloseAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<bool, Task>? OnCloseAsync { get; set; }

    /// <summary>The arguments of every <c>CloseAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<bool> CloseAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task CloseAsync(bool allowCommandsToComplete)
    {
        lock (CloseAsyncCalls) CloseAsyncCalls.Add(allowCommandsToComplete);
        if (OnCloseAsync is { } on)
        {
            return on(allowCommandsToComplete);
        }

        return Task.CompletedTask;
    }

    /// <summary>What <c>GetStormLog</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string?>? OnGetStormLog { get; set; }

    /// <summary>How many times <c>GetStormLog</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int GetStormLogCallCount => Volatile.Read(ref _getStormLogCallCount);

    /// <summary>Backs <see cref="GetStormLogCallCount"/>.</summary>
    private int _getStormLogCallCount;

    /// <inheritdoc/>
    public string? GetStormLog()
    {
        Interlocked.Increment(ref _getStormLogCallCount);
        if (OnGetStormLog is { } on)
        {
            return on();
        }

        return default;
    }

    /// <summary>What <c>ResetStormLog</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action? OnResetStormLog { get; set; }

    /// <summary>How many times <c>ResetStormLog</c> was called (counted atomically: a fake may be called from several threads).</summary>
    public int ResetStormLogCallCount => Volatile.Read(ref _resetStormLogCallCount);

    /// <summary>Backs <see cref="ResetStormLogCallCount"/>.</summary>
    private int _resetStormLogCallCount;

    /// <inheritdoc/>
    public void ResetStormLog()
    {
        Interlocked.Increment(ref _resetStormLogCallCount);
        OnResetStormLog?.Invoke();
    }

    /// <summary>What <c>PublishReconfigure</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CommandFlags, long>? OnPublishReconfigure { get; set; }

    /// <summary>The arguments of every <c>PublishReconfigure</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CommandFlags> PublishReconfigureCalls { get; } = [];

    /// <inheritdoc/>
    public long PublishReconfigure(CommandFlags flags)
    {
        lock (PublishReconfigureCalls) PublishReconfigureCalls.Add(flags);
        if (OnPublishReconfigure is { } on)
        {
            return on(flags);
        }

        return default;
    }

    /// <summary>What <c>PublishReconfigureAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<CommandFlags, Task<long>>? OnPublishReconfigureAsync { get; set; }

    /// <summary>The arguments of every <c>PublishReconfigureAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<CommandFlags> PublishReconfigureAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<long> PublishReconfigureAsync(CommandFlags flags)
    {
        lock (PublishReconfigureAsyncCalls) PublishReconfigureAsyncCalls.Add(flags);
        if (OnPublishReconfigureAsync is { } on)
        {
            return on(flags);
        }

        return Task.FromResult<long>(default);
    }

    /// <summary>What <c>GetHashSlot</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<RedisKey, int>? OnGetHashSlot { get; set; }

    /// <summary>The arguments of every <c>GetHashSlot</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<RedisKey> GetHashSlotCalls { get; } = [];

    /// <inheritdoc/>
    public int GetHashSlot(RedisKey key)
    {
        lock (GetHashSlotCalls) GetHashSlotCalls.Add(key);
        if (OnGetHashSlot is { } on)
        {
            return on(key);
        }

        return default;
    }

    /// <summary>What <c>ExportConfiguration</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<Stream, ExportOptions>? OnExportConfiguration { get; set; }

    /// <summary>The arguments of every <c>ExportConfiguration</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Stream Destination, ExportOptions Options)> ExportConfigurationCalls { get; } = [];

    /// <inheritdoc/>
    public void ExportConfiguration(Stream destination, ExportOptions options)
    {
        lock (ExportConfigurationCalls) ExportConfigurationCalls.Add((destination, options));
        OnExportConfiguration?.Invoke(destination, options);
    }

    /// <summary>What <c>AddLibraryNameSuffix</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Action<string>? OnAddLibraryNameSuffix { get; set; }

    /// <summary>The arguments of every <c>AddLibraryNameSuffix</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string> AddLibraryNameSuffixCalls { get; } = [];

    /// <inheritdoc/>
    public void AddLibraryNameSuffix(string suffix)
    {
        lock (AddLibraryNameSuffixCalls) AddLibraryNameSuffixCalls.Add(suffix);
        OnAddLibraryNameSuffix?.Invoke(suffix);
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
}
