using Maroik.FileStorage.Interfaces;

namespace Maroik.FileStorage.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IFileManagerStorageService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeFileManagerStorageService : IFileManagerStorageService
{
    /// <summary>What <c>AppendChunkAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string?, long, Stream, CancellationToken, Task<(string?, long)>>? OnAppendChunkAsync { get; set; }

    /// <summary>The arguments of every <c>AppendChunkAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string? UploadId, long Offset, Stream Chunk, CancellationToken Ct)> AppendChunkAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(string?, long)> AppendChunkAsync(string? uploadId, long offset, Stream chunk, CancellationToken ct)
    {
        lock (AppendChunkAsyncCalls) AppendChunkAsyncCalls.Add((uploadId, offset, chunk, ct));
        if (OnAppendChunkAsync is { } on)
        {
            return on(uploadId, offset, chunk, ct);
        }

        return Task.FromResult<(string?, long)>(default);
    }

    /// <summary>What <c>CompleteAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string?, long, bool, CancellationToken, Task<(string?, bool)>>? OnCompleteAsync { get; set; }

    /// <summary>The arguments of every <c>CompleteAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string? UploadId, long ExpectedSize, bool SkipScan, CancellationToken Ct)> CompleteAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(string?, bool)> CompleteAsync(string? uploadId, long expectedSize, bool skipScan, CancellationToken ct)
    {
        lock (CompleteAsyncCalls) CompleteAsyncCalls.Add((uploadId, expectedSize, skipScan, ct));
        if (OnCompleteAsync is { } on)
        {
            return on(uploadId, expectedSize, skipScan, ct);
        }

        return Task.FromResult<(string?, bool)>(default);
    }

    /// <summary>What <c>ScanAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<string?>>? OnScanAsync { get; set; }

    /// <summary>The arguments of every <c>ScanAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string UploadId, CancellationToken Ct)> ScanAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<string?> ScanAsync(string uploadId, CancellationToken ct)
    {
        lock (ScanAsyncCalls) ScanAsyncCalls.Add((uploadId, ct));
        if (OnScanAsync is { } on)
        {
            return on(uploadId, ct);
        }

        return Task.FromResult<string?>(default);
    }

    /// <summary>What <c>Abort</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string?, bool>? OnAbort { get; set; }

    /// <summary>The arguments of every <c>Abort</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string?> AbortCalls { get; } = [];

    /// <inheritdoc/>
    public bool Abort(string? uploadId)
    {
        lock (AbortCalls) AbortCalls.Add(uploadId);
        if (OnAbort is { } on)
        {
            return on(uploadId);
        }

        return default;
    }

    /// <summary>What <c>Delete</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string?, string?>? OnDelete { get; set; }

    /// <summary>The arguments of every <c>Delete</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<string?> DeleteCalls { get; } = [];

    /// <inheritdoc/>
    public string? Delete(string? filePath)
    {
        lock (DeleteCalls) DeleteCalls.Add(filePath);
        if (OnDelete is { } on)
        {
            return on(filePath);
        }

        return default;
    }
}
