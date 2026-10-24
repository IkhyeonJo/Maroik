namespace Maroik.FileStorage.Interfaces;

/// <summary>
/// The File Manager uploads waiting for, or in, their virus scan, and the refusals of the scans that ended without storing the file. It
/// lives only in memory: after a restart nothing is pending, and the next <c>complete</c> of an upload whose staging file is still there
/// queues its scan again. A completed upload is not scanned inside the request that completes it (a 2 GB file takes minutes, longer than
/// Cloudflare or an HTTP client waits); <c>FileManagerScanWorker</c> takes the scans from here in the background.
/// </summary>
public interface IFileManagerScanQueue
{
    /// <summary>Queues the scan of <paramref name="uploadId"/>; false (nothing queued) when it is already pending.</summary>
    bool TryEnqueue(string uploadId);

    /// <summary>Whether the scan of <paramref name="uploadId"/> is queued or running.</summary>
    bool IsPending(string uploadId);

    /// <summary>
    /// Waits for the next queued scan: its upload id, and the token that is cancelled when the upload is aborted
    /// (<see cref="Cancel"/>).
    /// </summary>
    ValueTask<(string UploadId, CancellationToken Aborted)> DequeueAsync(CancellationToken ct);

    /// <summary>
    /// Ends the pending scan of <paramref name="uploadId"/>. A <paramref name="refusal"/> (the file was not stored) is kept until
    /// <see cref="TakeRefusal"/> reads it, unless the upload was aborted meanwhile.
    /// </summary>
    void Finish(string uploadId, string? refusal);

    /// <summary>The refusal a finished scan of <paramref name="uploadId"/> left, or null; it is answered once.</summary>
    string? TakeRefusal(string uploadId);

    /// <summary>Cancels the pending scan of <paramref name="uploadId"/>, if any, and forgets its refusal.</summary>
    void Cancel(string uploadId);
}
