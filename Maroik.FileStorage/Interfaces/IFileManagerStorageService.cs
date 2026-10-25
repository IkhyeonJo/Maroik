namespace Maroik.FileStorage.Interfaces;

/// <summary>
/// The File Manager's large-file flow on disk (Implementation note [A]), kept apart from <see cref="IFileValidationService"/>, which
/// serves the small attachments: chunks are appended to a staging file, a completed upload is scanned from disk and moved to
/// <c>upload/FileManager/{GUID}</c>, an abandoned one is dropped, and a stored File Manager file can be deleted (the one exception to
/// the attachments' "no delete" rule, P4). A refusal is returned as one of the <c>FileStorageRefusals</c> texts.
/// </summary>
public interface IFileManagerStorageService
{
    /// <summary>
    /// Appends <paramref name="chunk"/> to the staging file of <paramref name="uploadId"/> when <paramref name="offset"/> equals the
    /// length received so far. Returns no error and the new length, or the refusal and the length received so far.
    /// </summary>
    Task<(string? Error, long Length)> AppendChunkAsync(string? uploadId, long offset, Stream chunk, CancellationToken ct = default);

    /// <summary>
    /// Completes the upload, and is asked again until it is decided (the scan runs in the background, <see cref="IFileManagerScanQueue"/>):
    /// a staging file of <paramref name="expectedSize"/> bytes gets its scan queued, and while it is queued or running the answer is
    /// "scanning"; afterwards it is "stored" (the file is in <c>upload/FileManager/{uploadId}</c>) or the scan's refusal, answered once.
    /// Returns no error and <c>Scanning</c> true while scanning, no error and false when stored, or the refusal.
    /// With <paramref name="skipScan"/> (the account's virus scan is off, decided by the Website) a whole staging file is moved there at
    /// once, unscanned, and the answer is "stored"; a scan already queued for the upload is still followed.
    /// </summary>
    Task<(string? Error, bool Scanning)> CompleteAsync(string? uploadId, long expectedSize, bool skipScan, CancellationToken ct = default);

    /// <summary>
    /// Scans the staging file of <paramref name="uploadId"/> with ClamAV and moves a clean file to <c>upload/FileManager/{uploadId}</c>.
    /// Returns null when stored, or the refusal (the staging file is then deleted). Throws <see cref="OperationCanceledException"/>
    /// without storing anything when <paramref name="ct"/> is cancelled (the upload was aborted, or the service stops).
    /// </summary>
    Task<string?> ScanAsync(string uploadId, CancellationToken ct);

    /// <summary>
    /// Drops what is held of an unconfirmed upload: cancels its pending scan, deletes its staging file, and deletes the file its scan
    /// already stored (the Website aborts only an upload it never confirmed). False for an id that is not a GUID.
    /// </summary>
    bool Abort(string? uploadId);

    /// <summary>Deletes the File Manager file at <paramref name="filePath"/>; returns null when deleted, or the refusal.</summary>
    string? Delete(string? filePath);
}
