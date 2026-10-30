using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IFileClient"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeFileClient : IFileClient
{
    /// <summary>When set, a call no rule or <c>On…</c> behaviour answers throws instead of returning a default value.</summary>
    public bool Strict { get; init; }

    /// <summary>What <c>DownloadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<byte[]>>? OnDownloadAsync { get; set; }

    /// <summary>The arguments of every <c>DownloadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string FilePath, string FileStorageBaseUrl, CancellationToken Ct)> DownloadAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerDownloadAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<byte[]>> Answer)> _downloadAsyncRules = [];

    /// <summary>Makes every later <c>DownloadAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDownloadAsync(Func<string, string, CancellationToken, Task<byte[]>> answer) => AnswerDownloadAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>DownloadAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDownloadAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<byte[]>> answer)
    {
        lock (_downloadAsyncRules) _downloadAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<byte[]> DownloadAsync(string filePath, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (DownloadAsyncCalls) DownloadAsyncCalls.Add((filePath, fileStorageBaseUrl, ct));
        Func<string, string, CancellationToken, Task<byte[]>>? answer = null;
        lock (_downloadAsyncRules) answer = _downloadAsyncRules.LastOrDefault(rule => rule.Match(filePath, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnDownloadAsync) is { } on)
        {
            return on(filePath, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("DownloadAsync was called without an answer on a strict fake.") : Task.FromResult<byte[]>([]);
    }

    /// <summary>What <c>OpenReadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<Stream>>? OnOpenReadAsync { get; set; }

    /// <summary>The arguments of every <c>OpenReadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string FilePath, string FileStorageBaseUrl, CancellationToken Ct)> OpenReadAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerOpenReadAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<Stream>> Answer)> _openReadAsyncRules = [];

    /// <summary>Makes every later <c>OpenReadAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerOpenReadAsync(Func<string, string, CancellationToken, Task<Stream>> answer) => AnswerOpenReadAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>OpenReadAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerOpenReadAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<Stream>> answer)
    {
        lock (_openReadAsyncRules) _openReadAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<Stream> OpenReadAsync(string filePath, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (OpenReadAsyncCalls) OpenReadAsyncCalls.Add((filePath, fileStorageBaseUrl, ct));
        Func<string, string, CancellationToken, Task<Stream>>? answer = null;
        lock (_openReadAsyncRules) answer = _openReadAsyncRules.LastOrDefault(rule => rule.Match(filePath, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnOpenReadAsync) is { } on)
        {
            return on(filePath, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("OpenReadAsync was called without an answer on a strict fake.") : Task.FromResult<Stream>(default!);
    }

    /// <summary>What <c>UploadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<byte[], string, string, string, CancellationToken, Task<bool>>? OnUploadAsync { get; set; }

    /// <summary>The arguments of every <c>UploadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(byte[] FileData, string ContentType, string FilePath, string FileStorageBaseUrl, CancellationToken Ct)> UploadAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUploadAsync</c> added, newest last.</summary>
    private readonly List<(Func<byte[], string, string, string, CancellationToken, bool> Match, Func<byte[], string, string, string, CancellationToken, Task<bool>> Answer)> _uploadAsyncRules = [];

    /// <summary>Makes every later <c>UploadAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUploadAsync(Func<byte[], string, string, string, CancellationToken, Task<bool>> answer) => AnswerUploadAsync((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UploadAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUploadAsync(Func<byte[], string, string, string, CancellationToken, bool> match, Func<byte[], string, string, string, CancellationToken, Task<bool>> answer)
    {
        lock (_uploadAsyncRules) _uploadAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<bool> UploadAsync(byte[] fileData, string contentType, string filePath, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (UploadAsyncCalls) UploadAsyncCalls.Add((fileData, contentType, filePath, fileStorageBaseUrl, ct));
        Func<byte[], string, string, string, CancellationToken, Task<bool>>? answer = null;
        lock (_uploadAsyncRules) answer = _uploadAsyncRules.LastOrDefault(rule => rule.Match(fileData, contentType, filePath, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnUploadAsync) is { } on)
        {
            return on(fileData, contentType, filePath, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("UploadAsync was called without an answer on a strict fake.") : Task.FromResult<bool>(default);
    }

    /// <summary>What <c>UploadWithResultAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<byte[], string, string, string, CancellationToken, Task<FileUploadResult>>? OnUploadWithResultAsync { get; set; }

    /// <summary>The arguments of every <c>UploadWithResultAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(byte[] FileData, string ContentType, string FilePath, string FileStorageBaseUrl, CancellationToken Ct)> UploadWithResultAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerUploadWithResultAsync</c> added, newest last.</summary>
    private readonly List<(Func<byte[], string, string, string, CancellationToken, bool> Match, Func<byte[], string, string, string, CancellationToken, Task<FileUploadResult>> Answer)> _uploadWithResultAsyncRules = [];

    /// <summary>Makes every later <c>UploadWithResultAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUploadWithResultAsync(Func<byte[], string, string, string, CancellationToken, Task<FileUploadResult>> answer) => AnswerUploadWithResultAsync((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>UploadWithResultAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerUploadWithResultAsync(Func<byte[], string, string, string, CancellationToken, bool> match, Func<byte[], string, string, string, CancellationToken, Task<FileUploadResult>> answer)
    {
        lock (_uploadWithResultAsyncRules) _uploadWithResultAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<FileUploadResult> UploadWithResultAsync(byte[] fileData, string contentType, string filePath, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (UploadWithResultAsyncCalls) UploadWithResultAsyncCalls.Add((fileData, contentType, filePath, fileStorageBaseUrl, ct));
        Func<byte[], string, string, string, CancellationToken, Task<FileUploadResult>>? answer = null;
        lock (_uploadWithResultAsyncRules) answer = _uploadWithResultAsyncRules.LastOrDefault(rule => rule.Match(fileData, contentType, filePath, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnUploadWithResultAsync) is { } on)
        {
            return on(fileData, contentType, filePath, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("UploadWithResultAsync was called without an answer on a strict fake.") : Task.FromResult<FileUploadResult>(default);
    }

    /// <summary>What <c>AppendFileManagerChunkAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Guid, long, Stream, string, CancellationToken, Task<long?>>? OnAppendFileManagerChunkAsync { get; set; }

    /// <summary>The arguments of every <c>AppendFileManagerChunkAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Guid UploadId, long Offset, Stream Chunk, string FileStorageBaseUrl, CancellationToken Ct)> AppendFileManagerChunkAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerAppendFileManagerChunkAsync</c> added, newest last.</summary>
    private readonly List<(Func<Guid, long, Stream, string, CancellationToken, bool> Match, Func<Guid, long, Stream, string, CancellationToken, Task<long?>> Answer)> _appendFileManagerChunkAsyncRules = [];

    /// <summary>Makes every later <c>AppendFileManagerChunkAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerAppendFileManagerChunkAsync(Func<Guid, long, Stream, string, CancellationToken, Task<long?>> answer) => AnswerAppendFileManagerChunkAsync((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>AppendFileManagerChunkAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerAppendFileManagerChunkAsync(Func<Guid, long, Stream, string, CancellationToken, bool> match, Func<Guid, long, Stream, string, CancellationToken, Task<long?>> answer)
    {
        lock (_appendFileManagerChunkAsyncRules) _appendFileManagerChunkAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<long?> AppendFileManagerChunkAsync(Guid uploadId, long offset, Stream chunk, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (AppendFileManagerChunkAsyncCalls) AppendFileManagerChunkAsyncCalls.Add((uploadId, offset, chunk, fileStorageBaseUrl, ct));
        Func<Guid, long, Stream, string, CancellationToken, Task<long?>>? answer = null;
        lock (_appendFileManagerChunkAsyncRules) answer = _appendFileManagerChunkAsyncRules.LastOrDefault(rule => rule.Match(uploadId, offset, chunk, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnAppendFileManagerChunkAsync) is { } on)
        {
            return on(uploadId, offset, chunk, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("AppendFileManagerChunkAsync was called without an answer on a strict fake.") : Task.FromResult<long?>(default);
    }

    /// <summary>What <c>CompleteFileManagerUploadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Guid, long, bool, string, CancellationToken, Task<FileUploadResult>>? OnCompleteFileManagerUploadAsync { get; set; }

    /// <summary>The arguments of every <c>CompleteFileManagerUploadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Guid UploadId, long ExpectedSize, bool SkipScan, string FileStorageBaseUrl, CancellationToken Ct)> CompleteFileManagerUploadAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerCompleteFileManagerUploadAsync</c> added, newest last.</summary>
    private readonly List<(Func<Guid, long, bool, string, CancellationToken, bool> Match, Func<Guid, long, bool, string, CancellationToken, Task<FileUploadResult>> Answer)> _completeFileManagerUploadAsyncRules = [];

    /// <summary>Makes every later <c>CompleteFileManagerUploadAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCompleteFileManagerUploadAsync(Func<Guid, long, bool, string, CancellationToken, Task<FileUploadResult>> answer) => AnswerCompleteFileManagerUploadAsync((_, _, _, _, _) => true, answer);

    /// <summary>Makes a later <c>CompleteFileManagerUploadAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerCompleteFileManagerUploadAsync(Func<Guid, long, bool, string, CancellationToken, bool> match, Func<Guid, long, bool, string, CancellationToken, Task<FileUploadResult>> answer)
    {
        lock (_completeFileManagerUploadAsyncRules) _completeFileManagerUploadAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<FileUploadResult> CompleteFileManagerUploadAsync(Guid uploadId, long expectedSize, bool skipScan, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (CompleteFileManagerUploadAsyncCalls) CompleteFileManagerUploadAsyncCalls.Add((uploadId, expectedSize, skipScan, fileStorageBaseUrl, ct));
        Func<Guid, long, bool, string, CancellationToken, Task<FileUploadResult>>? answer = null;
        lock (_completeFileManagerUploadAsyncRules) answer = _completeFileManagerUploadAsyncRules.LastOrDefault(rule => rule.Match(uploadId, expectedSize, skipScan, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnCompleteFileManagerUploadAsync) is { } on)
        {
            return on(uploadId, expectedSize, skipScan, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("CompleteFileManagerUploadAsync was called without an answer on a strict fake.") : Task.FromResult<FileUploadResult>(default);
    }

    /// <summary>What <c>AbortFileManagerUploadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<Guid, string, CancellationToken, Task<bool>>? OnAbortFileManagerUploadAsync { get; set; }

    /// <summary>The arguments of every <c>AbortFileManagerUploadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(Guid UploadId, string FileStorageBaseUrl, CancellationToken Ct)> AbortFileManagerUploadAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerAbortFileManagerUploadAsync</c> added, newest last.</summary>
    private readonly List<(Func<Guid, string, CancellationToken, bool> Match, Func<Guid, string, CancellationToken, Task<bool>> Answer)> _abortFileManagerUploadAsyncRules = [];

    /// <summary>Makes every later <c>AbortFileManagerUploadAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerAbortFileManagerUploadAsync(Func<Guid, string, CancellationToken, Task<bool>> answer) => AnswerAbortFileManagerUploadAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>AbortFileManagerUploadAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerAbortFileManagerUploadAsync(Func<Guid, string, CancellationToken, bool> match, Func<Guid, string, CancellationToken, Task<bool>> answer)
    {
        lock (_abortFileManagerUploadAsyncRules) _abortFileManagerUploadAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<bool> AbortFileManagerUploadAsync(Guid uploadId, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (AbortFileManagerUploadAsyncCalls) AbortFileManagerUploadAsyncCalls.Add((uploadId, fileStorageBaseUrl, ct));
        Func<Guid, string, CancellationToken, Task<bool>>? answer = null;
        lock (_abortFileManagerUploadAsyncRules) answer = _abortFileManagerUploadAsyncRules.LastOrDefault(rule => rule.Match(uploadId, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnAbortFileManagerUploadAsync) is { } on)
        {
            return on(uploadId, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("AbortFileManagerUploadAsync was called without an answer on a strict fake.") : Task.FromResult<bool>(default);
    }

    /// <summary>What <c>DeleteFileManagerFileAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string, CancellationToken, Task<bool>>? OnDeleteFileManagerFileAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteFileManagerFileAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string StoragePath, string FileStorageBaseUrl, CancellationToken Ct)> DeleteFileManagerFileAsyncCalls { get; } = [];

    /// <summary>The rules <c>AnswerDeleteFileManagerFileAsync</c> added, newest last.</summary>
    private readonly List<(Func<string, string, CancellationToken, bool> Match, Func<string, string, CancellationToken, Task<bool>> Answer)> _deleteFileManagerFileAsyncRules = [];

    /// <summary>Makes every later <c>DeleteFileManagerFileAsync</c> call run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteFileManagerFileAsync(Func<string, string, CancellationToken, Task<bool>> answer) => AnswerDeleteFileManagerFileAsync((_, _, _) => true, answer);

    /// <summary>Makes a later <c>DeleteFileManagerFileAsync</c> call whose arguments satisfy <paramref name="match"/> run <paramref name="answer"/> (a newer rule takes precedence).</summary>
    public void AnswerDeleteFileManagerFileAsync(Func<string, string, CancellationToken, bool> match, Func<string, string, CancellationToken, Task<bool>> answer)
    {
        lock (_deleteFileManagerFileAsyncRules) _deleteFileManagerFileAsyncRules.Add((match, answer));
    }

    /// <inheritdoc/>
    public Task<bool> DeleteFileManagerFileAsync(string storagePath, string fileStorageBaseUrl, CancellationToken ct)
    {
        lock (DeleteFileManagerFileAsyncCalls) DeleteFileManagerFileAsyncCalls.Add((storagePath, fileStorageBaseUrl, ct));
        Func<string, string, CancellationToken, Task<bool>>? answer = null;
        lock (_deleteFileManagerFileAsyncRules) answer = _deleteFileManagerFileAsyncRules.LastOrDefault(rule => rule.Match(storagePath, fileStorageBaseUrl, ct)).Answer;
        if ((answer ?? OnDeleteFileManagerFileAsync) is { } on)
        {
            return on(storagePath, fileStorageBaseUrl, ct);
        }

        return Strict ? throw new InvalidOperationException("DeleteFileManagerFileAsync was called without an answer on a strict fake.") : Task.FromResult<bool>(default);
    }

    /// <summary>How many calls of every member were recorded, all together.</summary>
    public int TotalCallCount => DownloadAsyncCalls.Count + OpenReadAsyncCalls.Count + UploadAsyncCalls.Count + UploadWithResultAsyncCalls.Count + AppendFileManagerChunkAsyncCalls.Count + CompleteFileManagerUploadAsyncCalls.Count + AbortFileManagerUploadAsyncCalls.Count + DeleteFileManagerFileAsyncCalls.Count;

    /// <summary>Forgets every recorded call (the answers stay).</summary>
    public void ClearCalls()
    {
        lock (DownloadAsyncCalls) DownloadAsyncCalls.Clear();
        lock (OpenReadAsyncCalls) OpenReadAsyncCalls.Clear();
        lock (UploadAsyncCalls) UploadAsyncCalls.Clear();
        lock (UploadWithResultAsyncCalls) UploadWithResultAsyncCalls.Clear();
        lock (AppendFileManagerChunkAsyncCalls) AppendFileManagerChunkAsyncCalls.Clear();
        lock (CompleteFileManagerUploadAsyncCalls) CompleteFileManagerUploadAsyncCalls.Clear();
        lock (AbortFileManagerUploadAsyncCalls) AbortFileManagerUploadAsyncCalls.Clear();
        lock (DeleteFileManagerFileAsyncCalls) DeleteFileManagerFileAsyncCalls.Clear();
    }
}
