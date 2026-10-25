using System.Threading.Channels;
using Maroik.FileStorage.Interfaces;

namespace Maroik.FileStorage.Services;

/// <inheritdoc cref="IFileManagerScanQueue" />
/// <remarks>
/// One lock guards the pending, refused and undecided uploads together, so a <c>complete</c> that finds an upload no longer pending
/// always finds its refusal (or its stored file): <see cref="Finish"/> records the refusal and ends the pending scan in one step. Each pending upload
/// owns the source of its abort token; it is disposed when the scan finishes.
/// </remarks>
public sealed class FileManagerScanQueue : IFileManagerScanQueue
{
    /// <summary>Guards <see cref="_pending"/>, <see cref="_refusals"/> and <see cref="_undecided"/>.</summary>
    private readonly Lock _gate = new();

    /// <summary>The queued or running scans, with the source of the token their abort cancels.</summary>
    private readonly Dictionary<string, CancellationTokenSource> _pending = new(StringComparer.Ordinal);

    /// <summary>The refusals of the finished scans not yet answered.</summary>
    private readonly Dictionary<string, string> _refusals = new(StringComparer.Ordinal);

    /// <summary>
    /// The refusals of the scans that could not be finished, whose files wait for their owners' decision; answered until
    /// <see cref="TakeUndecided"/> or <see cref="Cancel"/> removes them.
    /// </summary>
    private readonly Dictionary<string, string> _undecided = new(StringComparer.Ordinal);

    /// <summary>The upload ids in the order they were queued.</summary>
    private readonly Channel<string> _queued = Channel.CreateUnbounded<string>();

    /// <inheritdoc />
    public bool TryEnqueue(string uploadId)
    {
        lock (_gate)
        {
            if (_pending.ContainsKey(uploadId))
                return false;

            _pending[uploadId] = new CancellationTokenSource();
        }

        // An unbounded channel always takes the write.
        return _queued.Writer.TryWrite(uploadId);
    }

    /// <inheritdoc />
    public bool IsPending(string uploadId)
    {
        lock (_gate)
            return _pending.ContainsKey(uploadId);
    }

    /// <inheritdoc />
    public async ValueTask<(string UploadId, CancellationToken Aborted)> DequeueAsync(CancellationToken ct)
    {
        string uploadId = await _queued.Reader.ReadAsync(ct);
        lock (_gate)
            return (uploadId, _pending[uploadId].Token);
    }

    /// <inheritdoc />
    public void Finish(string uploadId, string? refusal, bool awaitsDecision = false)
    {
        lock (_gate)
        {
            // Only a dequeued upload is finished, so it is always pending here.
            _pending.Remove(uploadId, out CancellationTokenSource? aborted);
            if (refusal != null && aborted is { IsCancellationRequested: false })
                (awaitsDecision ? _undecided : _refusals)[uploadId] = refusal;
            aborted?.Dispose();
        }
    }

    /// <inheritdoc />
    public string? TakeRefusal(string uploadId)
    {
        lock (_gate)
            return _refusals.Remove(uploadId, out string? refusal) ? refusal : _undecided.GetValueOrDefault(uploadId);
    }

    /// <inheritdoc />
    public bool TakeUndecided(string uploadId)
    {
        lock (_gate)
            return _undecided.Remove(uploadId);
    }

    /// <inheritdoc />
    public void Cancel(string uploadId)
    {
        lock (_gate)
        {
            _refusals.Remove(uploadId);
            _undecided.Remove(uploadId);
            if (_pending.TryGetValue(uploadId, out CancellationTokenSource? aborted))
                aborted.Cancel();
        }
    }
}
