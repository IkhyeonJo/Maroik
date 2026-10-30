using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Worker.Tests.Fixtures;

/// <summary>A hand-written stand-in for <see cref="IFileManagerService"/>: each member records its calls and runs the behaviour a test sets (<c>On…</c>); without one it returns an empty / default value.</summary>
public sealed class FakeFileManagerService : IFileManagerService
{
    /// <summary>What <c>GetDriveAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, CancellationToken, Task<FileManagerDriveResponse>>? OnGetDriveAsync { get; set; }

    /// <summary>The arguments of every <c>GetDriveAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, CancellationToken Ct)> GetDriveAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<FileManagerDriveResponse> GetDriveAsync(string accountEmail, CancellationToken ct)
    {
        lock (GetDriveAsyncCalls) GetDriveAsyncCalls.Add((accountEmail, ct));
        if (OnGetDriveAsync is { } on)
        {
            return on(accountEmail, ct);
        }

        return Task.FromResult<FileManagerDriveResponse>(default!);
    }

    /// <summary>What <c>GetFolderAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long?, CancellationToken, Task<(ServiceResult, FileManagerFolderResponse?)>>? OnGetFolderAsync { get; set; }

    /// <summary>The arguments of every <c>GetFolderAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long? FolderId, CancellationToken Ct)> GetFolderAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, FileManagerFolderResponse?)> GetFolderAsync(string accountEmail, long? folderId, CancellationToken ct)
    {
        lock (GetFolderAsyncCalls) GetFolderAsyncCalls.Add((accountEmail, folderId, ct));
        if (OnGetFolderAsync is { } on)
        {
            return on(accountEmail, folderId, ct);
        }

        return Task.FromResult<(ServiceResult, FileManagerFolderResponse?)>(default);
    }

    /// <summary>What <c>SearchAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, string?, CancellationToken, Task<List<FileManagerItemResponse>>>? OnSearchAsync { get; set; }

    /// <summary>The arguments of every <c>SearchAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, string? Query, CancellationToken Ct)> SearchAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<List<FileManagerItemResponse>> SearchAsync(string accountEmail, string? query, CancellationToken ct)
    {
        lock (SearchAsyncCalls) SearchAsyncCalls.Add((accountEmail, query, ct));
        if (OnSearchAsync is { } on)
        {
            return on(accountEmail, query, ct);
        }

        return Task.FromResult<List<FileManagerItemResponse>>([]);
    }

    /// <summary>What <c>CreateFolderAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long?, string?, bool, CancellationToken, Task<(ServiceResult, long?)>>? OnCreateFolderAsync { get; set; }

    /// <summary>The arguments of every <c>CreateFolderAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long? ParentId, string? Name, bool Merge, CancellationToken Ct)> CreateFolderAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, long?)> CreateFolderAsync(string accountEmail, long? parentId, string? name, bool merge, CancellationToken ct)
    {
        lock (CreateFolderAsyncCalls) CreateFolderAsyncCalls.Add((accountEmail, parentId, name, merge, ct));
        if (OnCreateFolderAsync is { } on)
        {
            return on(accountEmail, parentId, name, merge, ct);
        }

        return Task.FromResult<(ServiceResult, long?)>(default);
    }

    /// <summary>What <c>RenameAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long, string?, CancellationToken, Task<ServiceResult>>? OnRenameAsync { get; set; }

    /// <summary>The arguments of every <c>RenameAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long Id, string? Name, CancellationToken Ct)> RenameAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> RenameAsync(string accountEmail, long id, string? name, CancellationToken ct)
    {
        lock (RenameAsyncCalls) RenameAsyncCalls.Add((accountEmail, id, name, ct));
        if (OnRenameAsync is { } on)
        {
            return on(accountEmail, id, name, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>MoveAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IReadOnlyCollection<long>, long?, CancellationToken, Task<ServiceResult>>? OnMoveAsync { get; set; }

    /// <summary>The arguments of every <c>MoveAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, IReadOnlyCollection<long> Ids, long? TargetFolderId, CancellationToken Ct)> MoveAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> MoveAsync(string accountEmail, IReadOnlyCollection<long> ids, long? targetFolderId, CancellationToken ct)
    {
        lock (MoveAsyncCalls) MoveAsyncCalls.Add((accountEmail, ids, targetFolderId, ct));
        if (OnMoveAsync is { } on)
        {
            return on(accountEmail, ids, targetFolderId, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>DeleteAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IReadOnlyCollection<long>, CancellationToken, Task<ServiceResult>>? OnDeleteAsync { get; set; }

    /// <summary>The arguments of every <c>DeleteAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, IReadOnlyCollection<long> Ids, CancellationToken Ct)> DeleteAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> DeleteAsync(string accountEmail, IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        lock (DeleteAsyncCalls) DeleteAsyncCalls.Add((accountEmail, ids, ct));
        if (OnDeleteAsync is { } on)
        {
            return on(accountEmail, ids, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>CheckQuotaAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long, CancellationToken, Task<ServiceResult>>? OnCheckQuotaAsync { get; set; }

    /// <summary>The arguments of every <c>CheckQuotaAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long TotalBytes, CancellationToken Ct)> CheckQuotaAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> CheckQuotaAsync(string accountEmail, long totalBytes, CancellationToken ct)
    {
        lock (CheckQuotaAsyncCalls) CheckQuotaAsyncCalls.Add((accountEmail, totalBytes, ct));
        if (OnCheckQuotaAsync is { } on)
        {
            return on(accountEmail, totalBytes, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>BeginUploadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, FileManagerUploadRequest, CancellationToken, Task<(ServiceResult, FileManagerUploadStart?)>>? OnBeginUploadAsync { get; set; }

    /// <summary>The arguments of every <c>BeginUploadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, FileManagerUploadRequest Request, CancellationToken Ct)> BeginUploadAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, FileManagerUploadStart?)> BeginUploadAsync(string accountEmail, FileManagerUploadRequest request, CancellationToken ct)
    {
        lock (BeginUploadAsyncCalls) BeginUploadAsyncCalls.Add((accountEmail, request, ct));
        if (OnBeginUploadAsync is { } on)
        {
            return on(accountEmail, request, ct);
        }

        return Task.FromResult<(ServiceResult, FileManagerUploadStart?)>(default);
    }

    /// <summary>What <c>AppendChunkAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, Guid, long, Stream, long, CancellationToken, Task<(ServiceResult, long)>>? OnAppendChunkAsync { get; set; }

    /// <summary>The arguments of every <c>AppendChunkAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, Guid UploadId, long Offset, Stream Chunk, long ChunkLength, CancellationToken Ct)> AppendChunkAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, long)> AppendChunkAsync(string accountEmail, Guid uploadId, long offset, Stream chunk, long chunkLength, CancellationToken ct)
    {
        lock (AppendChunkAsyncCalls) AppendChunkAsyncCalls.Add((accountEmail, uploadId, offset, chunk, chunkLength, ct));
        if (OnAppendChunkAsync is { } on)
        {
            return on(accountEmail, uploadId, offset, chunk, chunkLength, ct);
        }

        return Task.FromResult<(ServiceResult, long)>(default);
    }

    /// <summary>What <c>CompleteUploadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, Guid, bool, CancellationToken, Task<(ServiceResult, FileManagerCompletion)>>? OnCompleteUploadAsync { get; set; }

    /// <summary>The arguments of every <c>CompleteUploadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, Guid UploadId, bool Overwrite, CancellationToken Ct)> CompleteUploadAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, FileManagerCompletion)> CompleteUploadAsync(string accountEmail, Guid uploadId, bool overwrite, CancellationToken ct)
    {
        lock (CompleteUploadAsyncCalls) CompleteUploadAsyncCalls.Add((accountEmail, uploadId, overwrite, ct));
        if (OnCompleteUploadAsync is { } on)
        {
            return on(accountEmail, uploadId, overwrite, ct);
        }

        return Task.FromResult<(ServiceResult, FileManagerCompletion)>(default);
    }

    /// <summary>What <c>AbortUploadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, Guid, CancellationToken, Task<ServiceResult>>? OnAbortUploadAsync { get; set; }

    /// <summary>The arguments of every <c>AbortUploadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, Guid UploadId, CancellationToken Ct)> AbortUploadAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<ServiceResult> AbortUploadAsync(string accountEmail, Guid uploadId, CancellationToken ct)
    {
        lock (AbortUploadAsyncCalls) AbortUploadAsyncCalls.Add((accountEmail, uploadId, ct));
        if (OnAbortUploadAsync is { } on)
        {
            return on(accountEmail, uploadId, ct);
        }

        return Task.FromResult<ServiceResult>(default!);
    }

    /// <summary>What <c>RemoveStaleUploadsAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<int, CancellationToken, Task<int>>? OnRemoveStaleUploadsAsync { get; set; }

    /// <summary>The arguments of every <c>RemoveStaleUploadsAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(int MaxAccounts, CancellationToken Ct)> RemoveStaleUploadsAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<int> RemoveStaleUploadsAsync(int maxAccounts, CancellationToken ct)
    {
        lock (RemoveStaleUploadsAsyncCalls) RemoveStaleUploadsAsyncCalls.Add((maxAccounts, ct));
        if (OnRemoveStaleUploadsAsync is { } on)
        {
            return on(maxAccounts, ct);
        }

        return Task.FromResult<int>(default);
    }

    /// <summary>What <c>OpenFileAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long, CancellationToken, Task<(ServiceResult, AttachmentDownload?)>>? OnOpenFileAsync { get; set; }

    /// <summary>The arguments of every <c>OpenFileAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long Id, CancellationToken Ct)> OpenFileAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, AttachmentDownload?)> OpenFileAsync(string accountEmail, long id, CancellationToken ct)
    {
        lock (OpenFileAsyncCalls) OpenFileAsyncCalls.Add((accountEmail, id, ct));
        if (OnOpenFileAsync is { } on)
        {
            return on(accountEmail, id, ct);
        }

        return Task.FromResult<(ServiceResult, AttachmentDownload?)>(default);
    }

    /// <summary>What <c>OpenPreviewAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, long, CancellationToken, Task<(ServiceResult, FileManagerPreview?)>>? OnOpenPreviewAsync { get; set; }

    /// <summary>The arguments of every <c>OpenPreviewAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, long Id, CancellationToken Ct)> OpenPreviewAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, FileManagerPreview?)> OpenPreviewAsync(string accountEmail, long id, CancellationToken ct)
    {
        lock (OpenPreviewAsyncCalls) OpenPreviewAsyncCalls.Add((accountEmail, id, ct));
        if (OnOpenPreviewAsync is { } on)
        {
            return on(accountEmail, id, ct);
        }

        return Task.FromResult<(ServiceResult, FileManagerPreview?)>(default);
    }

    /// <summary>What <c>ListFilesForDownloadAsync</c> does; <see langword="null"/> returns an empty / default value.</summary>
    public Func<string, IReadOnlyCollection<long>, CancellationToken, Task<(ServiceResult, List<FileManagerItemResponse>)>>? OnListFilesForDownloadAsync { get; set; }

    /// <summary>The arguments of every <c>ListFilesForDownloadAsync</c> call, in order (appended under a lock on the list: a fake may be called from several threads).</summary>
    public List<(string AccountEmail, IReadOnlyCollection<long> Ids, CancellationToken Ct)> ListFilesForDownloadAsyncCalls { get; } = [];

    /// <inheritdoc/>
    public Task<(ServiceResult, List<FileManagerItemResponse>)> ListFilesForDownloadAsync(string accountEmail, IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        lock (ListFilesForDownloadAsyncCalls) ListFilesForDownloadAsyncCalls.Add((accountEmail, ids, ct));
        if (OnListFilesForDownloadAsync is { } on)
        {
            return on(accountEmail, ids, ct);
        }

        return Task.FromResult<(ServiceResult, List<FileManagerItemResponse>)>(default);
    }
}
