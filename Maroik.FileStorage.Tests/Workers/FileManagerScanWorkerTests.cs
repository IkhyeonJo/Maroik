using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Interfaces;
using Maroik.FileStorage.Services;
using Maroik.FileStorage.Tests.Fixtures;
using Maroik.FileStorage.Workers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Maroik.FileStorage.Tests.Workers;

/// <summary>
/// Unit tests for <see cref="FileManagerScanWorker"/>, which runs the queued File Manager scans in the background: a queued upload is
/// scanned through a scoped <see cref="IFileManagerStorageService"/> and finished with its outcome, at most
/// <see cref="FileManagerScanWorker.MaxConcurrentScans"/> run at once, and a scan that fails or is aborted is logged and ends the upload
/// without keeping it pending.
/// </summary>
public sealed class FileManagerScanWorkerTests : IAsyncDisposable
{
    /// <summary>The real queue the worker takes from.</summary>
    private readonly FileManagerScanQueue _queue = new();

    /// <summary>The storage service the worker scans through.</summary>
    private readonly FakeFileManagerStorageService _storage = new();

    /// <summary>The worker's captured log.</summary>
    private readonly FakeLogger<FileManagerScanWorker> _logger = new();

    /// <summary>The service provider the worker creates its scopes from.</summary>
    private readonly ServiceProvider _services;

    /// <summary>The worker under test, started by <see cref="StartAsync"/>.</summary>
    private readonly FileManagerScanWorker _worker;

    /// <summary>Builds the worker over the real queue and the fake storage service.</summary>
    public FileManagerScanWorkerTests()
    {
        _services = new ServiceCollection().AddScoped<IFileManagerStorageService>(_ => _storage).BuildServiceProvider();
        _worker = new FileManagerScanWorker(_queue, _services.GetRequiredService<IServiceScopeFactory>(), TimeProvider.System, _logger);
    }

    /// <summary>Stops the worker and releases the provider.</summary>
    public async ValueTask DisposeAsync()
    {
        await _worker.StopAsync(CancellationToken.None);
        _worker.Dispose();
        await _services.DisposeAsync();
    }

    /// <summary>Starts the worker.</summary>
    private Task StartAsync() => _worker.StartAsync(TestContext.Current.CancellationToken);

    /// <summary>Waits until <paramref name="condition"/> holds (the worker runs on its own).</summary>
    private static async Task EventuallyAsync(Func<bool> condition)
    {
        for (int i = 0; i < 500 && !condition(); i++)
            await Task.Delay(10, TestContext.Current.CancellationToken);
        Assert.True(condition());
    }

    /// <summary>A fresh upload id.</summary>
    private static string NewId() => Guid.NewGuid().ToString("D");

    /// <summary>A queued upload is scanned and finished: a clean scan leaves no refusal, a refused one leaves its refusal; both are logged.</summary>
    [Fact]
    public async Task AQueuedUpload_IsScanned_AndFinishedWithItsOutcome()
    {
        string clean = NewId();
        string infected = NewId();
        _storage.OnScanAsync = (id, _) => Task.FromResult(id == infected ? FileStorageRefusals.Infected : null);
        await StartAsync();

        _queue.TryEnqueue(clean);
        _queue.TryEnqueue(infected);
        await EventuallyAsync(() => !_queue.IsPending(clean) && !_queue.IsPending(infected));

        Assert.Null(_queue.TakeRefusal(clean));
        Assert.Equal(FileStorageRefusals.Infected, _queue.TakeRefusal(infected));
        var log = _logger.Collector.GetSnapshot();
        Assert.Contains(log, r => r.Level == LogLevel.Information && r.Message == $"File Manager scan of {clean} started");
        Assert.Contains(log, r => r.Level == LogLevel.Information && r.Message.StartsWith($"File Manager scan of {clean} stored the file after ", StringComparison.Ordinal));
        Assert.Contains(log, r => r.Level == LogLevel.Information
                                  && r.Message.StartsWith($"File Manager scan of {infected} refused the file ({FileStorageRefusals.Infected}) after ", StringComparison.Ordinal));
    }

    /// <summary>At most <see cref="FileManagerScanWorker.MaxConcurrentScans"/> scans run at once; the next starts when one ends.</summary>
    [Fact]
    public async Task AtMostTheConcurrencyLimit_RunAtOnce()
    {
        int running = 0;
        int most = 0;
        var release = new TaskCompletionSource();
        _storage.OnScanAsync = async (_, _) =>
        {
            most = Math.Max(most, Interlocked.Increment(ref running));
            await release.Task;
            Interlocked.Decrement(ref running);
            return null;
        };
        await StartAsync();
        string[] ids = [NewId(), NewId(), NewId()];

        foreach (string id in ids) _queue.TryEnqueue(id);
        await EventuallyAsync(() => Volatile.Read(ref running) == FileManagerScanWorker.MaxConcurrentScans);
        await Task.Delay(100, TestContext.Current.CancellationToken);
        Assert.Equal(FileManagerScanWorker.MaxConcurrentScans, Volatile.Read(ref running));

        release.SetResult();
        await EventuallyAsync(() => ids.All(id => !_queue.IsPending(id)));
        Assert.Equal(2, FileManagerScanWorker.MaxConcurrentScans);
        Assert.Equal(FileManagerScanWorker.MaxConcurrentScans, most);
    }

    /// <summary>A scan that throws is logged at Error with the exception, ends the upload with a failure refusal, and the worker goes on.</summary>
    [Fact]
    public async Task AFailedScan_IsLogged_AndEndsTheUploadWithAFailure()
    {
        string failing = NewId();
        string next = NewId();
        _storage.OnScanAsync = (id, _) => id == failing ? Task.FromException<string?>(new IOException("disk")) : Task.FromResult<string?>(null);
        await StartAsync();

        _queue.TryEnqueue(failing);
        _queue.TryEnqueue(next);
        await EventuallyAsync(() => !_queue.IsPending(failing) && !_queue.IsPending(next));

        Assert.Equal(FileStorageRefusals.StoreFailed, _queue.TakeRefusal(failing));
        FakeLogRecord error = Assert.Single(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Equal($"File Manager scan of {failing} failed", error.Message);
        Assert.Equal("disk", error.Exception?.Message);
    }

    /// <summary>A scan whose upload is aborted is cancelled, logged at Information, and leaves no refusal.</summary>
    [Fact]
    public async Task AnAbortedScan_IsCancelled_AndLeavesNoRefusal()
    {
        string id = NewId();
        var started = new TaskCompletionSource();
        _storage.OnScanAsync = async (_, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return null;
        };
        await StartAsync();

        _queue.TryEnqueue(id);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        _queue.Cancel(id);
        await EventuallyAsync(() => !_queue.IsPending(id));

        Assert.Null(_queue.TakeRefusal(id));
        Assert.Contains(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information && r.Message == $"File Manager scan of {id} was cancelled");
        Assert.DoesNotContain(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
    }

    /// <summary>Stopping the service ends the scans that are running; they stay unfinished on disk for the next complete to queue again.</summary>
    [Fact]
    public async Task Stopping_EndsTheRunningScans()
    {
        string id = NewId();
        var started = new TaskCompletionSource();
        _storage.OnScanAsync = async (_, ct) =>
        {
            started.SetResult();
            await Task.Delay(Timeout.Infinite, ct);
            return null;
        };
        await StartAsync();
        _queue.TryEnqueue(id);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);

        await _worker.StopAsync(TestContext.Current.CancellationToken);

        Assert.Contains(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information && r.Message == $"File Manager scan of {id} was cancelled");
        Assert.DoesNotContain(_logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
    }
}
