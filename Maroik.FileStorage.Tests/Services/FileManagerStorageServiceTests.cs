using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Services;
using Maroik.FileStorage.Settings;
using Maroik.FileStorage.Tests.Fixtures;
using Microsoft.Extensions.Options;

namespace Maroik.FileStorage.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FileManagerStorageService"/>, the File Manager's large-file flow (Implementation note [A]), against a real
/// throwaway storage root on disk, a real scan queue and a fake ClamAV client: chunks are appended to a staging file in offset order;
/// completing an upload checks the size and queues its scan, then reports "scanning", "stored" or the refusal; the scan streams
/// the file from disk and moves a clean one to <c>upload/FileManager/{GUID}</c>; an abort cancels the scan and drops the
/// staging (and an unconfirmed stored) file; and a delete removes only a file directly in <c>upload/FileManager/</c>.
/// </summary>
public sealed class FileManagerStorageServiceTests : IDisposable
{
    /// <summary>The throwaway parent of the storage root.</summary>
    private readonly string _base = Path.Combine(Path.GetTempPath(), "fm-" + Guid.NewGuid().ToString("N"));

    /// <summary>The storage root (<c>{base}/upload</c>), as the service is configured with it.</summary>
    private string Root => Path.Combine(_base, "upload");

    /// <summary>The ClamAV client the service scans with.</summary>
    private readonly FakeClamavClient _clamav = new();

    /// <summary>The upload id these tests use.</summary>
    private static readonly Guid UploadId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    /// <summary>Creates the storage root; the ClamAV fake answers Clean unless a test says otherwise.</summary>
    public FileManagerStorageServiceTests()
    {
        Directory.CreateDirectory(Root);
        Scans(ClamavScanResult.Clean);
    }

    /// <summary>Removes the throwaway storage.</summary>
    public void Dispose() => Directory.Delete(_base, recursive: true);

    /// <summary>Makes every scan answer <paramref name="result"/>, recording what was scanned.</summary>
    private void Scans(ClamavScanResult result, List<byte[]>? scanned = null) =>
        _clamav.OnScanWithClamavAsyncWithScanTimeout = async (s, _, _, _, c) =>
            {
                using var copy = new MemoryStream();
                s.Position = 0;
                await s.CopyToAsync(copy, c);
                scanned?.Add(copy.ToArray());
                return result;
            };

    /// <summary>The scan queue the service queues on (a real one).</summary>
    private readonly FileManagerScanQueue _queue = new();

    /// <summary>The service under test, over the throwaway root.</summary>
    private FileManagerStorageService Sut => new(_clamav, _queue, Options.Create(new FileStorageSetting { StorageRootPath = Root, ClamavHost = "clamav", ClamavPort = 3310 }));

    /// <summary>The staging file of <paramref name="id"/>.</summary>
    private string Staging(Guid id) => Path.Combine(Root, "FileManager", ".staging", id.ToString("D"));

    /// <summary>The stored file of <paramref name="id"/>.</summary>
    private string Stored(Guid id) => Path.Combine(Root, "FileManager", id.ToString("D"));

    /// <summary>Appends <paramref name="bytes"/> at <paramref name="offset"/>.</summary>
    private Task<(string? Error, long Length)> AppendAsync(long offset, params byte[] bytes) =>
        Sut.AppendChunkAsync(UploadId.ToString("D"), offset, new MemoryStream(bytes), TestContext.Current.CancellationToken);

    // -- AppendChunkAsync ---------------------------------------------------------

    /// <summary>The first chunk creates the staging file and each next chunk at the current length is appended to it.</summary>
    [Fact]
    public async Task AppendChunkAsync_AppendsChunks_InOffsetOrder()
    {
        Assert.Equal((null, 3L), await AppendAsync(0, 1, 2, 3));
        Assert.Equal((null, 5L), await AppendAsync(3, 4, 5));

        Assert.Equal([1, 2, 3, 4, 5], await File.ReadAllBytesAsync(Staging(UploadId), TestContext.Current.CancellationToken));
    }

    /// <summary>A chunk whose offset is not the current length (a resend, a gap) is refused with the current length, and nothing is written.</summary>
    [Theory]
    [InlineData(0L)]
    [InlineData(5L)]
    public async Task AppendChunkAsync_RefusesAChunkAtAnotherOffset(long offset)
    {
        await AppendAsync(0, 1, 2, 3);

        Assert.Equal((FileStorageRefusals.OffsetMismatch, 3L), await AppendAsync(offset, 9, 9));
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Staging(UploadId), TestContext.Current.CancellationToken));
    }

    /// <summary>An upload id that is not a GUID (a path, a traversal) is refused before anything touches the disk.</summary>
    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData(null)]
    public async Task AppendChunkAsync_RefusesAnUploadIdThatIsNotAGuid(string? uploadId)
    {
        var result = await Sut.AppendChunkAsync(uploadId, 0, new MemoryStream([1]), TestContext.Current.CancellationToken);

        Assert.Equal((FileStorageRefusals.InvalidUpload, 0L), result);
        Assert.False(Directory.Exists(Path.Combine(Root, "FileManager")));
    }

    // -- CompleteAsync ------------------------------------------------------------

    /// <summary>The id <see cref="UploadId"/> as the requests carry it.</summary>
    private static string Id => UploadId.ToString("D");

    /// <summary>Completes <see cref="UploadId"/> as declared <paramref name="expectedSize"/> bytes.</summary>
    private Task<(string? Error, bool Scanning)> CompleteAsync(long expectedSize, bool skipScan = false) =>
        Sut.CompleteAsync(Id, expectedSize, skipScan, TestContext.Current.CancellationToken);

    /// <summary>
    /// A complete upload of the declared size is not scanned in the request: its scan is queued, the answer is "scanning", and the
    /// staging file stays where the scan will read it.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_QueuesTheScan_AndAnswersScanning()
    {
        await AppendAsync(0, 1, 2, 3);

        Assert.Equal((null, true), await CompleteAsync(3));

        Assert.True(_queue.IsPending(Id));
        Assert.True(File.Exists(Staging(UploadId)));
        Assert.False(File.Exists(Stored(UploadId)));
        Assert.Empty(_clamav.ScanWithClamavAsyncWithScanTimeoutCalls);
    }

    /// <summary>
    /// The account's virus scan is off (skipScan): the whole staging file is stored at once — no scan queued, ClamAV never called — and
    /// complete answers "stored"; asking again answers the same.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithoutAScan_StoresTheFileAtOnce()
    {
        await AppendAsync(0, 1, 2, 3);

        Assert.Equal((null, false), await CompleteAsync(3, skipScan: true));

        Assert.False(_queue.IsPending(Id));
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Stored(UploadId), TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Staging(UploadId)));
        Assert.Equal((null, false), await CompleteAsync(3, skipScan: true));
        Assert.Empty(_clamav.ScanWithClamavAsyncWithScanTimeoutCalls);
    }

    /// <summary>Without a scan an empty file is stored too, and a size mismatch is still refused (the staging file is dropped).</summary>
    [Fact]
    public async Task CompleteAsync_WithoutAScan_StoresAnEmptyFile_AndRefusesASizeMismatch()
    {
        Assert.Equal((null, false), await CompleteAsync(0, skipScan: true));
        Assert.Empty(await File.ReadAllBytesAsync(Stored(UploadId), TestContext.Current.CancellationToken));

        Guid other = Guid.NewGuid();
        await Sut.AppendChunkAsync(other.ToString("D"), 0, new MemoryStream([1, 2]), TestContext.Current.CancellationToken);
        Assert.Equal((FileStorageRefusals.SizeMismatch, false), await Sut.CompleteAsync(other.ToString("D"), 3, true, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Stored(other)));
        Assert.False(File.Exists(Staging(other)));
    }

    /// <summary>
    /// An upload whose scan was already queued (the scan was turned off after it completed) follows that scan: "scanning", nothing stored.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WithoutAScan_FollowsAScanAlreadyQueued()
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);

        Assert.Equal((null, true), await CompleteAsync(3, skipScan: true));
        Assert.False(File.Exists(Stored(UploadId)));
    }

    /// <summary>A complete while the scan is pending answers "scanning" again and queues nothing more.</summary>
    [Fact]
    public async Task CompleteAsync_WhileTheScanIsPending_AnswersScanning()
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);
        await _queue.DequeueAsync(TestContext.Current.CancellationToken);

        Assert.Equal((null, true), await CompleteAsync(3));
        using var nothingElse = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        nothingElse.CancelAfter(TimeSpan.FromMilliseconds(100));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await _queue.DequeueAsync(nothingElse.Token));
    }

    /// <summary>Once the scan stored the file, complete answers "stored".</summary>
    [Fact]
    public async Task CompleteAsync_AfterTheScanStoredTheFile_AnswersStored()
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);
        await _queue.DequeueAsync(TestContext.Current.CancellationToken);
        await Sut.ScanAsync(Id, TestContext.Current.CancellationToken);
        _queue.Finish(Id, null);

        Assert.Equal((null, false), await CompleteAsync(3));
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Stored(UploadId), TestContext.Current.CancellationToken));
    }

    /// <summary>Once the scan refused the file, complete answers that refusal once.</summary>
    [Fact]
    public async Task CompleteAsync_AfterTheScanRefusedTheFile_AnswersTheRefusalOnce()
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);
        await _queue.DequeueAsync(TestContext.Current.CancellationToken);
        _queue.Finish(Id, FileStorageRefusals.Infected);
        File.Delete(Staging(UploadId));

        Assert.Equal((FileStorageRefusals.Infected, false), await CompleteAsync(3));
        Assert.Equal((FileStorageRefusals.SizeMismatch, false), await CompleteAsync(3));
    }

    /// <summary>
    /// After a restart nothing is pending: a complete of an upload whose staging file is still whole queues its scan again.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_AfterARestart_QueuesTheScanAgain()
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);

        var restarted = new FileManagerScanQueue();
        var service = new FileManagerStorageService(_clamav, restarted, Options.Create(new FileStorageSetting { StorageRootPath = Root }));

        Assert.Equal((null, true), await service.CompleteAsync(Id, 3, false, TestContext.Current.CancellationToken));
        Assert.True(restarted.IsPending(Id));
    }

    /// <summary>An empty file (no chunk was sent) gets an empty staging file and is queued too.</summary>
    [Fact]
    public async Task CompleteAsync_QueuesAnEmptyFile()
    {
        Assert.Equal((null, true), await CompleteAsync(0));

        Assert.Empty(await File.ReadAllBytesAsync(Staging(UploadId), TestContext.Current.CancellationToken));
    }

    /// <summary>A staging file of another size than declared is refused without a scan and deleted.</summary>
    [Fact]
    public async Task CompleteAsync_RefusesASizeMismatch()
    {
        await AppendAsync(0, 1, 2, 3);

        Assert.Equal((FileStorageRefusals.SizeMismatch, false), await CompleteAsync(4));
        Assert.False(File.Exists(Staging(UploadId)));
        Assert.False(_queue.IsPending(Id));
    }

    /// <summary>A file never overwrites one already stored under the same GUID: a staging file beside it is refused and deleted.</summary>
    [Fact]
    public async Task CompleteAsync_NeverOverwritesAStoredFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Stored(UploadId))!);
        await File.WriteAllBytesAsync(Stored(UploadId), [7], TestContext.Current.CancellationToken);
        await AppendAsync(0, 1);

        Assert.Equal((FileStorageRefusals.InvalidUpload, false), await CompleteAsync(1));
        Assert.Equal([7], await File.ReadAllBytesAsync(Stored(UploadId), TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Staging(UploadId)));
    }

    /// <summary>An invalid upload id is refused.</summary>
    [Fact]
    public async Task CompleteAsync_RefusesAnUploadIdThatIsNotAGuid()
    {
        Assert.Equal((FileStorageRefusals.InvalidUpload, false), await Sut.CompleteAsync("../x", 0, true, TestContext.Current.CancellationToken));
    }

    // -- ScanAsync ----------------------------------------------------------------

    /// <summary>
    /// The scan streams the staging file to ClamAV with the File Manager's long time limit, then moves a clean file to
    /// upload/FileManager/{GUID}; the staging file is gone.
    /// </summary>
    [Fact]
    public async Task ScanAsync_ScansAndStoresTheFile()
    {
        List<byte[]> scanned = [];
        Scans(ClamavScanResult.Clean, scanned);
        await AppendAsync(0, 1, 2, 3);

        Assert.Null(await Sut.ScanAsync(Id, TestContext.Current.CancellationToken));

        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Stored(UploadId), TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Staging(UploadId)));
        Assert.Equal([1, 2, 3], Assert.Single(scanned));
        var scanCall = Assert.Single(_clamav.ScanWithClamavAsyncWithScanTimeoutCalls);
        Assert.Equal(("clamav", 3310, FileManagerStorageService.ScanTimeout), (scanCall.ClamavHost, scanCall.ClamavPort, scanCall.ScanTimeout));
        Assert.True(FileManagerStorageService.ScanTimeout >= TimeSpan.FromMinutes(10));
    }

    /// <summary>
    /// An infected file, or one the scan could not finish (a clamd limit, or the scanner unavailable), is not stored: the refusal
    /// says which, and the staging file is deleted.
    /// </summary>
    [Theory]
    [InlineData(ClamavScanResult.Infected, FileStorageRefusals.Infected)]
    [InlineData(ClamavScanResult.LimitExceeded, FileStorageRefusals.ScanLimitExceeded)]
    [InlineData(ClamavScanResult.Unavailable, FileStorageRefusals.ScanUnavailable)]
    public async Task ScanAsync_RefusesAnInfectedOrUnscannableFile_AndDeletesIt(ClamavScanResult scan, string refusal)
    {
        Scans(scan);
        await AppendAsync(0, 1, 2, 3);

        Assert.Equal(refusal, await Sut.ScanAsync(Id, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Stored(UploadId)));
        Assert.False(File.Exists(Staging(UploadId)));
    }

    /// <summary>The two "could not finish" refusals are told apart from each other by their text.</summary>
    [Fact]
    public void TheCouldNotScanRefusals_AreDistinct()
    {
        Assert.DoesNotContain(FileStorageRefusals.ScanUnavailable, FileStorageRefusals.ScanLimitExceeded, StringComparison.Ordinal);
        Assert.DoesNotContain(FileStorageRefusals.ScanLimitExceeded, FileStorageRefusals.ScanUnavailable, StringComparison.Ordinal);
    }

    /// <summary>A scan of an id that is not a GUID is refused before anything touches the disk.</summary>
    [Fact]
    public async Task ScanAsync_RefusesAnUploadIdThatIsNotAGuid()
    {
        Assert.Equal(FileStorageRefusals.InvalidUpload, await Sut.ScanAsync("../x", TestContext.Current.CancellationToken));
        Assert.Empty(_clamav.ScanWithClamavAsyncWithScanTimeoutCalls);
    }

    /// <summary>A scan whose upload was aborted meanwhile stores nothing and ends cancelled.</summary>
    [Fact]
    public async Task ScanAsync_StoresNothing_WhenTheUploadWasAborted()
    {
        await AppendAsync(0, 1, 2, 3);
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Sut.ScanAsync(Id, aborted.Token));
        Assert.False(File.Exists(Stored(UploadId)));
    }

    // -- Abort --------------------------------------------------------------------

    /// <summary>An abort deletes the staging file; aborting an upload that has none is fine; an invalid id is refused.</summary>
    [Fact]
    public async Task Abort_DeletesTheStagingFile()
    {
        await AppendAsync(0, 1, 2);

        Assert.True(Sut.Abort(Id));
        Assert.False(File.Exists(Staging(UploadId)));
        Assert.True(Sut.Abort(Guid.NewGuid().ToString("D")));
        Assert.False(Sut.Abort("../x"));
    }

    /// <summary>
    /// An abort while the file is scanning cancels the scan; an abort after the scan stored it (the upload was never confirmed)
    /// deletes the stored file too.
    /// </summary>
    [Fact]
    public async Task Abort_CancelsThePendingScan_AndDeletesAFileItStored()
    {
        await AppendAsync(0, 1, 2);
        Assert.Equal((null, true), await CompleteAsync(2));
        (_, CancellationToken scanAborted) = await _queue.DequeueAsync(TestContext.Current.CancellationToken);
        Directory.CreateDirectory(Path.GetDirectoryName(Stored(UploadId))!);
        await File.WriteAllBytesAsync(Stored(UploadId), [1, 2, 3], TestContext.Current.CancellationToken);

        Assert.True(Sut.Abort(Id));

        Assert.True(scanAborted.IsCancellationRequested);
        Assert.False(File.Exists(Staging(UploadId)));
        Assert.False(File.Exists(Stored(UploadId)));
    }

    // -- Delete -------------------------------------------------------------------

    /// <summary>A stored File Manager file is deleted.</summary>
    [Fact]
    public async Task Delete_RemovesAFileManagerFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Stored(UploadId))!);
        await File.WriteAllBytesAsync(Stored(UploadId), [1], TestContext.Current.CancellationToken);

        Assert.Null(Sut.Delete($"upload/FileManager/{UploadId:D}"));
        Assert.False(File.Exists(Stored(UploadId)));
    }

    /// <summary>A missing file is reported as not found.</summary>
    [Fact]
    public void Delete_ReportsAMissingFile()
    {
        Assert.Equal(FileStorageRefusals.FileNotFound, Sut.Delete($"upload/FileManager/{UploadId:D}"));
    }

    /// <summary>
    /// Only a GUID-named file directly in upload/FileManager/ can be deleted: an attachment, a staging file, a traversal or a name that is
    /// not a GUID is refused and the file stays (the attachments keep their "no delete API" rule).
    /// </summary>
    [Theory]
    [InlineData("upload/Forum/a.zip")]
    [InlineData("upload/FileManager/.staging/0f8fad5b-d9cb-469f-a165-70867728950e")]
    [InlineData("upload/FileManager/../Forum/a.zip")]
    [InlineData("upload/FileManager/notes.txt")]
    [InlineData("/etc/passwd")]
    [InlineData("")]
    [InlineData(null)]
    public async Task Delete_RefusesAnythingButAFileManagerFile(string? path)
    {
        string forum = Path.Combine(Root, "Forum", "a.zip");
        Directory.CreateDirectory(Path.GetDirectoryName(forum)!);
        await File.WriteAllBytesAsync(forum, [1], TestContext.Current.CancellationToken);

        Assert.Equal(FileStorageRefusals.NotAFileManagerFile, Sut.Delete(path));
        Assert.True(File.Exists(forum));
    }
}
