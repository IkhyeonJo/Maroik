using System.Collections.Concurrent;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;

namespace Maroik.Website.Tests.Fixtures;

/// <summary>
/// The website's real file-storage client with the class-suggestion model files kept in memory, registered by
/// <see cref="MaroikWebApplicationFactory"/> (there is no file storage service in the website tests): a model is saved, read and deleted
/// here; every other call goes to <paramref name="inner"/> unchanged.
/// </summary>
public sealed class ClassModelFileClient(IFileClient inner) : IFileClient
{
    /// <summary>The stored model files by path.</summary>
    private readonly ConcurrentDictionary<string, byte[]> _models = new();

    /// <inheritdoc />
    public Task<byte[]> DownloadAsync(string filePath, CancellationToken ct = default) =>
        _models.TryGetValue(filePath, out byte[]? model) ? Task.FromResult(model) : inner.DownloadAsync(filePath, ct);

    /// <inheritdoc />
    public Task<Stream> OpenReadAsync(string filePath, CancellationToken ct = default) => inner.OpenReadAsync(filePath, ct);

    /// <inheritdoc />
    public Task<bool> UploadAsync(byte[] fileData, string contentType, string filePath, CancellationToken ct = default) =>
        inner.UploadAsync(fileData, contentType, filePath, ct);

    /// <inheritdoc />
    public Task<FileUploadResult> UploadWithResultAsync(byte[] fileData, string contentType, string filePath, CancellationToken ct = default) =>
        inner.UploadWithResultAsync(fileData, contentType, filePath, ct);

    /// <inheritdoc />
    public Task<long?> AppendFileManagerChunkAsync(Guid uploadId, long offset, Stream chunk, CancellationToken ct = default) =>
        inner.AppendFileManagerChunkAsync(uploadId, offset, chunk, ct);

    /// <inheritdoc />
    public Task<FileUploadResult> CompleteFileManagerUploadAsync(Guid uploadId, long expectedSize, bool skipScan, CancellationToken ct = default) =>
        inner.CompleteFileManagerUploadAsync(uploadId, expectedSize, skipScan, ct);

    /// <inheritdoc />
    public Task<bool> AbortFileManagerUploadAsync(Guid uploadId, CancellationToken ct = default) => inner.AbortFileManagerUploadAsync(uploadId, ct);

    /// <inheritdoc />
    public Task<bool> DeleteFileManagerFileAsync(string storagePath, CancellationToken ct = default) => inner.DeleteFileManagerFileAsync(storagePath, ct);

    /// <inheritdoc />
    public Task<bool> SaveClassModelAsync(string storagePath, byte[] model, CancellationToken ct = default) =>
        Task.FromResult(_models.TryAdd(storagePath, model));

    /// <inheritdoc />
    public Task<bool> DeleteClassModelAsync(string storagePath, CancellationToken ct = default) =>
        Task.FromResult(_models.TryRemove(storagePath, out _));
}
