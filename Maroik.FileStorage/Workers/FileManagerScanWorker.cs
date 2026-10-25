using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Interfaces;

namespace Maroik.FileStorage.Workers;

/// <summary>
/// Runs the File Manager scans queued by <c>complete</c> (<see cref="IFileManagerScanQueue"/>) in the background, so no request waits for
/// one: ClamAV takes minutes over a large archive (a 484 MB zip of installers took 202 seconds), while an HTTP client gives up after 100
/// seconds and Cloudflare answers 524 after 100 seconds too. <see cref="MaxConcurrentScans"/> loops take from the queue, so a batch
/// upload's next file can be scanning while the one after it is still being sent, and clamd is never handed more than that at once.
/// Each scan gets a scope of its own (the storage service and its ClamAV client are scoped) and runs with the upload's abort token
/// linked to the service's stopping token. Its outcome is recorded for the next <c>complete</c> to answer — a scan that could not be
/// finished (<see cref="FileStorageRefusals.AwaitsDecision"/>) leaves the upload waiting for its owner's decision; a scan that throws
/// is logged and ends as <see cref="FileStorageRefusals.StoreFailed"/>. A scan the service stopping cuts short leaves its staging file whole,
/// and the next <c>complete</c> queues it again.
/// </summary>
public sealed class FileManagerScanWorker(IFileManagerScanQueue queue, IServiceScopeFactory scopes, TimeProvider timeProvider, ILogger<FileManagerScanWorker> logger)
    : BackgroundService
{
    /// <summary>The most scans that run at once (D2).</summary>
    public const int MaxConcurrentScans = 2;

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        Task.WhenAll(Enumerable.Range(0, MaxConcurrentScans).Select(_ => RunAsync(stoppingToken)));

    /// <summary>Takes one queued scan after another until the service stops.</summary>
    private async Task RunAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            string uploadId;
            CancellationToken aborted;
            try
            {
                (uploadId, aborted) = await queue.DequeueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // The service is stopping: nothing was taken from the queue.
                return;
            }

            await ScanAsync(uploadId, aborted, stoppingToken);
        }
    }

    /// <summary>Scans one upload and records its outcome.</summary>
    private async Task ScanAsync(string uploadId, CancellationToken aborted, CancellationToken stoppingToken)
    {
        using var scan = CancellationTokenSource.CreateLinkedTokenSource(aborted, stoppingToken);
        long started = timeProvider.GetTimestamp();
        string? refusal;
        try
        {
            logger.LogInformation("File Manager scan of {UploadId} started", uploadId);
            await using AsyncServiceScope scope = scopes.CreateAsyncScope();
            refusal = await scope.ServiceProvider.GetRequiredService<IFileManagerStorageService>().ScanAsync(uploadId, scan.Token);
        }
        catch (OperationCanceledException) when (scan.IsCancellationRequested)
        {
            // The upload was aborted (its files are already dropped) or the service is stopping (the staging file stays for the next
            // complete): neither is a failure, and no outcome is recorded.
            logger.LogInformation("File Manager scan of {UploadId} was cancelled", uploadId);
            queue.Finish(uploadId, null);
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "File Manager scan of {UploadId} failed", uploadId);
            queue.Finish(uploadId, FileStorageRefusals.StoreFailed);
            return;
        }

        TimeSpan elapsed = timeProvider.GetElapsedTime(started);
        bool awaitsDecision = FileStorageRefusals.AwaitsDecision(refusal);
        if (refusal == null)
            logger.LogInformation("File Manager scan of {UploadId} stored the file after {Elapsed}", uploadId, elapsed);
        else if (awaitsDecision)
            logger.LogInformation("File Manager scan of {UploadId} could not be finished ({Reason}) after {Elapsed}; it awaits its owner's decision", uploadId, refusal, elapsed);
        else
            logger.LogInformation("File Manager scan of {UploadId} refused the file ({Reason}) after {Elapsed}", uploadId, refusal, elapsed);
        queue.Finish(uploadId, refusal, awaitsDecision);
    }
}
