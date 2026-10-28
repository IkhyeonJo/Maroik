using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Helpers;
using Maroik.FileStorage.Interfaces;
using Maroik.FileStorage.Settings;
using Microsoft.Extensions.Options;

namespace Maroik.FileStorage.Services;

/// <summary>
/// The File Manager's large-file flow on disk. No file is held in memory: chunks are appended to
/// <c>upload/FileManager/.staging/{uploadId}</c>, a completed file of the declared size gets its scan queued
/// (<see cref="IFileManagerScanQueue"/>; <c>FileManagerScanWorker</c> runs it in the background), the scan streams it from disk, and
/// only a clean file is moved to <c>upload/FileManager/{uploadId}</c> — or, for an account whose virus scan the Website says is off,
/// the completed file is moved there at once, unscanned. The upload id is the GUID the Website stores the file under; anything
/// that is not a GUID is refused before the disk is touched, so an id can never name a path. Every path goes through
/// <see cref="StoragePath"/> and <see cref="StorageFiles"/> like the rest of the service.
/// </summary>
public class FileManagerStorageService(IClamavClient clamavClient, IFileManagerScanQueue scanQueue, IOptions<FileStorageSetting> settings)
    : IFileManagerStorageService
{
    /// <summary>
    /// The time limit of one File Manager scan. A scanned file reaches 2 GB (a larger one is never sent to clamd), far beyond an
    /// attachment, so the client's 30-second default would refuse it; clamd's own MaxScanTime (set in the compose files) ends a scan that runs longer as "could not be scanned".
    /// </summary>
    public static readonly TimeSpan ScanTimeout = TimeSpan.FromMinutes(15);

    /// <summary>The directory the stored File Manager files live in.</summary>
    private const string FileManagerDirectory = "upload/FileManager";

    /// <summary>The directory the unfinished uploads' staging files live in.</summary>
    private const string StagingDirectory = "upload/FileManager/.staging";

    /// <summary>The upload id in its canonical form, or null when it is not a GUID.</summary>
    private static string? CanonicalId(string? uploadId) =>
        Guid.TryParseExact(uploadId, "D", out Guid id) ? id.ToString("D") : null;

    /// <summary><paramref name="relativeDirectory"/> confined to the storage root.</summary>
    private StoragePath? Directory(string relativeDirectory) =>
        StoragePath.ResolveWithinRoot(settings.Value.StorageRootPath, relativeDirectory);

    /// <summary>The staging file of <paramref name="uploadId"/>, or null for an id that is not a GUID.</summary>
    private StoragePath? StagingFile(string? uploadId) =>
        CanonicalId(uploadId) is { } id ? Directory(StagingDirectory)?.InDirectory(id) : null;

    /// <summary>The stored file of <paramref name="uploadId"/>, or null for an id that is not a GUID.</summary>
    private StoragePath? StoredFile(string? uploadId) =>
        CanonicalId(uploadId) is { } id ? Directory(FileManagerDirectory)?.InDirectory(id) : null;

    /// <inheritdoc />
    public async Task<(string? Error, long Length)> AppendChunkAsync(string? uploadId, long offset, Stream chunk, CancellationToken ct = default)
    {
        StoragePath? staging = StagingFile(uploadId);
        StoragePath? stagingDirectory = Directory(StagingDirectory);
        if (staging == null || stagingDirectory == null)
            return (FileStorageRefusals.InvalidUpload, 0);

        StorageFiles.CreateDirectory(stagingDirectory);
        long received = StorageFiles.Exists(staging) ? StorageFiles.Length(staging) : 0;
        // Chunks arrive one at a time in order; a resent or skipped chunk would corrupt the file, so only the next one is taken.
        if (offset != received)
            return (FileStorageRefusals.OffsetMismatch, received);

        await using (Stream file = StorageFiles.OpenAppend(staging))
        {
            await chunk.CopyToAsync(file, ct);
            await file.FlushAsync(ct);
        }

        return (null, StorageFiles.Length(staging));
    }

    /// <inheritdoc />
    public async Task<(string? Error, bool Scanning)> CompleteAsync(string? uploadId, long expectedSize, bool skipScan, CancellationToken ct = default)
    {
        string? id = CanonicalId(uploadId);
        StoragePath? staging = StagingFile(id);
        StoragePath? stored = StoredFile(id);
        StoragePath? stagingDirectory = Directory(StagingDirectory);
        if (id == null || staging == null || stored == null || stagingDirectory == null)
            return (FileStorageRefusals.InvalidUpload, false);

        // The queue records a refusal before it ends the pending scan, and a clean scan moves the file before it, so an upload that is
        // no longer pending has its refusal or its stored file in place by now.
        if (scanQueue.IsPending(id))
            return (null, true);
        if (scanQueue.TakeRefusal(id) is { } refusal)
            return (refusal, false);

        bool staged = StorageFiles.Exists(staging);
        if (StorageFiles.Exists(stored))
        {
            if (!staged)
                return (null, false);

            // Every upload gets a fresh GUID, so a stored file beside a new staging file means something is wrong upstream: never
            // overwrite it.
            StorageFiles.Delete(staging);
            return (FileStorageRefusals.InvalidUpload, false);
        }

        // An empty file sends no chunk: its staging file is created here.
        if (expectedSize == 0 && !staged)
        {
            StorageFiles.CreateDirectory(stagingDirectory);
            await using Stream empty = StorageFiles.OpenAppend(staging);
            staged = true;
        }

        if (!staged)
            return (FileStorageRefusals.SizeMismatch, false);

        if (StorageFiles.Length(staging) != expectedSize)
        {
            StorageFiles.Delete(staging);
            return (FileStorageRefusals.SizeMismatch, false);
        }

        // The account's virus scan is off (the Website decides): the whole file is stored as it is.
        if (skipScan)
        {
            StorageFiles.Move(staging, stored);
            StorageFiles.MakeReadable(stored);
            return (null, false);
        }

        // Queued here the first time, and again after a restart forgot the pending scan (its staging file is still whole).
        scanQueue.TryEnqueue(id);
        return (null, true);
    }

    /// <inheritdoc />
    public async Task<string?> ScanAsync(string uploadId, CancellationToken ct)
    {
        StoragePath? staging = StagingFile(uploadId);
        StoragePath? stored = StoredFile(uploadId);
        if (staging == null || stored == null)
            return FileStorageRefusals.InvalidUpload;

        ClamavScanResult scan;
        await using (Stream file = StorageFiles.OpenRead(staging))
            scan = await clamavClient.ScanWithClamavAsync(file, settings.Value.ClamavHost ?? "", settings.Value.ClamavPort, ScanTimeout, ct);

        // An abort cancels the scan, which the client reports as "could not be scanned": the abort already dropped the files.
        ct.ThrowIfCancellationRequested();

        // Only Clean is stored. Infected, too large to scan and "could not be scanned" are all refusals, reported apart.
        if (scan != ClamavScanResult.Clean)
        {
            StorageFiles.Delete(staging);
            return scan switch
            {
                ClamavScanResult.Infected => FileStorageRefusals.Infected,
                ClamavScanResult.LimitExceeded => FileStorageRefusals.ScanLimitExceeded,
                _ => FileStorageRefusals.ScanUnavailable
            };
        }

        StorageFiles.Move(staging, stored);
        StorageFiles.MakeReadable(stored);
        return null;
    }

    /// <inheritdoc />
    public bool Abort(string? uploadId)
    {
        string? id = CanonicalId(uploadId);
        StoragePath? staging = StagingFile(id);
        StoragePath? stored = StoredFile(id);
        if (id == null || staging == null || stored == null)
            return false;

        scanQueue.Cancel(id);
        StorageFiles.Delete(staging);
        StorageFiles.Delete(stored);
        return true;
    }

    /// <inheritdoc />
    public string? Delete(string? filePath)
    {
        // Only "upload/FileManager/{GUID}" itself: an attachment, a staging file or a traversal is not a File Manager file.
        string prefix = FileManagerDirectory + "/";
        string? id = filePath != null && filePath.StartsWith(prefix, StringComparison.Ordinal) ? CanonicalId(filePath[prefix.Length..]) : null;
        StoragePath? stored = id != null && filePath == prefix + id ? StoredFile(id) : null;
        if (stored == null)
            return FileStorageRefusals.NotAFileManagerFile;

        if (!StorageFiles.Exists(stored))
            return FileStorageRefusals.FileNotFound;

        StorageFiles.Delete(stored);
        return null;
    }
}
