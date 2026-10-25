using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Messages;
using Maroik.FileStorage.Services;
using Maroik.FileStorage.Settings;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.FileStorage.Tests.Services;

/// <summary>
/// Unit tests for <see cref="FileManagerStorageService"/>, the File Manager's large-file flow (Implementation note [A]), against a real
/// throwaway storage root on disk, a real scan queue and a mocked ClamAV client: chunks are appended to a staging file in offset order
/// (P5); completing an upload checks the size and queues its scan, then reports "scanning", "stored" or the refusal; the scan streams
/// the file from disk and moves a clean one to <c>upload/FileManager/{GUID}</c> (P1, S2); an abort cancels the scan and drops the
/// staging (and an unconfirmed stored) file; and a delete removes only a file directly in <c>upload/FileManager/</c> (P4).
/// </summary>
public sealed class FileManagerStorageServiceTests : IDisposable
{
    /// <summary>The throwaway parent of the storage root.</summary>
    private readonly string _base = Path.Combine(Path.GetTempPath(), "fm-" + Guid.NewGuid().ToString("N"));

    /// <summary>The storage root (<c>{base}/upload</c>), as the service is configured with it.</summary>
    private string Root => Path.Combine(_base, "upload");

    /// <summary>The ClamAV client the service scans with.</summary>
    private readonly Mock<IClamavClient> _clamav = new();

    /// <summary>The upload id these tests use.</summary>
    private static readonly Guid UploadId = Guid.Parse("0f8fad5b-d9cb-469f-a165-70867728950e");

    /// <summary>Creates the storage root; the ClamAV mock answers Clean unless a test says otherwise.</summary>
    public FileManagerStorageServiceTests()
    {
        Directory.CreateDirectory(Root);
        Scans(ClamavScanResult.Clean);
    }

    /// <summary>Removes the throwaway storage.</summary>
    public void Dispose() => Directory.Delete(_base, recursive: true);

    /// <summary>Makes every scan answer <paramref name="result"/>, recording what was scanned.</summary>
    private void Scans(ClamavScanResult result, List<byte[]>? scanned = null) =>
        _clamav.Setup(c => c.ScanWithClamavAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(async (Stream s, string _, int _, TimeSpan _, CancellationToken c) =>
            {
                using var copy = new MemoryStream();
                s.Position = 0;
                await s.CopyToAsync(copy, c);
                scanned?.Add(copy.ToArray());
                return result;
            });

    /// <summary>The scan queue the service queues on (a real one).</summary>
    private readonly FileManagerScanQueue _queue = new();

    /// <summary>The service under test, over the throwaway root.</summary>
    private FileManagerStorageService Sut => new(_clamav.Object, _queue, Options.Create(new FileStorageSetting { StorageRootPath = Root, ClamavHost = "clamav", ClamavPort = 3310 }));

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
    private Task<(string? Error, bool Scanning)> CompleteAsync(long expectedSize) =>
        Sut.CompleteAsync(Id, expectedSize, TestContext.Current.CancellationToken);

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
        _clamav.Verify(c => c.ScanWithClamavAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
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
    /// A file whose scan could not be finished waits for its owner's decision: every complete answers the same refusal (the page may ask
    /// again), nothing is queued again, and the staging file stays.
    /// </summary>
    [Fact]
    public async Task CompleteAsync_WhileTheOwnerDecides_AnswersTheRefusalEveryTime()
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);
        await _queue.DequeueAsync(TestContext.Current.CancellationToken);
        _queue.Finish(Id, FileStorageRefusals.ScanLimitExceeded, awaitsDecision: true);

        Assert.Equal((FileStorageRefusals.ScanLimitExceeded, false), await CompleteAsync(3));
        Assert.Equal((FileStorageRefusals.ScanLimitExceeded, false), await CompleteAsync(3));
        Assert.False(_queue.IsPending(Id));
        Assert.True(File.Exists(Staging(UploadId)));
    }

    /// <summary>
    /// After a restart nothing is pending: a complete of an upload whose staging file is still whole queues its scan again (D5).
    /// </summary>
    [Fact]
    public async Task CompleteAsync_AfterARestart_QueuesTheScanAgain()
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);

        var restarted = new FileManagerScanQueue();
        var service = new FileManagerStorageService(_clamav.Object, restarted, Options.Create(new FileStorageSetting { StorageRootPath = Root }));

        Assert.Equal((null, true), await service.CompleteAsync(Id, 3, TestContext.Current.CancellationToken));
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
        Assert.Equal((FileStorageRefusals.InvalidUpload, false), await Sut.CompleteAsync("../x", 0, TestContext.Current.CancellationToken));
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
        _clamav.Verify(c => c.ScanWithClamavAsync(It.IsAny<Stream>(), "clamav", 3310, FileManagerStorageService.ScanTimeout, It.IsAny<CancellationToken>()), Times.Once);
        Assert.True(FileManagerStorageService.ScanTimeout >= TimeSpan.FromMinutes(10));
    }

    /// <summary>An infected file is not stored (S2): the refusal says so, and the staging file is deleted.</summary>
    [Fact]
    public async Task ScanAsync_RefusesAnInfectedFile_AndDeletesIt()
    {
        Scans(ClamavScanResult.Infected);
        await AppendAsync(0, 1, 2, 3);

        Assert.Equal(FileStorageRefusals.Infected, await Sut.ScanAsync(Id, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Stored(UploadId)));
        Assert.False(File.Exists(Staging(UploadId)));
    }

    /// <summary>
    /// A file the scan could not finish (a clamd limit, or the scanner unavailable) is not stored, but its staging file is kept: its owner
    /// may still choose to keep it unscanned (<see cref="FileManagerStorageService.AcceptUnscanned"/>). The refusal says which.
    /// </summary>
    [Theory]
    [InlineData(ClamavScanResult.LimitExceeded, FileStorageRefusals.ScanLimitExceeded)]
    [InlineData(ClamavScanResult.Unavailable, FileStorageRefusals.ScanUnavailable)]
    public async Task ScanAsync_KeepsAFileTheScanCouldNotFinish_ForItsOwnersDecision(ClamavScanResult scan, string refusal)
    {
        Scans(scan);
        await AppendAsync(0, 1, 2, 3);

        Assert.Equal(refusal, await Sut.ScanAsync(Id, TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Stored(UploadId)));
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Staging(UploadId), TestContext.Current.CancellationToken));
    }

    /// <summary>The two "could not finish" refusals are told apart from every other refusal, and from each other, by their text.</summary>
    [Fact]
    public void TheUndecidedRefusals_AreDistinct()
    {
        Assert.True(FileStorageRefusals.AwaitsDecision(FileStorageRefusals.ScanLimitExceeded));
        Assert.True(FileStorageRefusals.AwaitsDecision(FileStorageRefusals.ScanUnavailable));
        Assert.False(FileStorageRefusals.AwaitsDecision(FileStorageRefusals.Infected));
        Assert.False(FileStorageRefusals.AwaitsDecision(FileStorageRefusals.StoreFailed));
        Assert.DoesNotContain(FileStorageRefusals.ScanUnavailable, FileStorageRefusals.ScanLimitExceeded, StringComparison.Ordinal);
        Assert.DoesNotContain(FileStorageRefusals.ScanLimitExceeded, FileStorageRefusals.ScanUnavailable, StringComparison.Ordinal);
    }

    /// <summary>A scan of an id that is not a GUID is refused before anything touches the disk.</summary>
    [Fact]
    public async Task ScanAsync_RefusesAnUploadIdThatIsNotAGuid()
    {
        Assert.Equal(FileStorageRefusals.InvalidUpload, await Sut.ScanAsync("../x", TestContext.Current.CancellationToken));
        _clamav.Verify(c => c.ScanWithClamavAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
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

    // -- AcceptUnscanned -----------------------------------------------------------

    /// <summary>Brings <see cref="UploadId"/> to "waiting for its owner's decision" after a scan that could not be finished.</summary>
    private async Task UndecidedAsync(string refusal = FileStorageRefusals.ScanLimitExceeded)
    {
        await AppendAsync(0, 1, 2, 3);
        await CompleteAsync(3);
        await _queue.DequeueAsync(TestContext.Current.CancellationToken);
        _queue.Finish(Id, refusal, awaitsDecision: true);
    }

    /// <summary>
    /// The owner keeps a file the scan could not finish (either reason): it is moved to upload/FileManager/{GUID} unscanned, the staging
    /// file is gone, and complete now answers "stored".
    /// </summary>
    [Theory]
    [InlineData(FileStorageRefusals.ScanLimitExceeded)]
    [InlineData(FileStorageRefusals.ScanUnavailable)]
    public async Task AcceptUnscanned_StoresAFileWaitingForTheDecision(string refusal)
    {
        await UndecidedAsync(refusal);

        Assert.Null(Sut.AcceptUnscanned(Id));

        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Stored(UploadId), TestContext.Current.CancellationToken));
        Assert.False(File.Exists(Staging(UploadId)));
        Assert.Equal((null, false), await CompleteAsync(3));
    }

    /// <summary>Accepting again once the file is stored answers "stored" (the Website may repeat a call whose answer it lost).</summary>
    [Fact]
    public async Task AcceptUnscanned_IsIdempotent()
    {
        await UndecidedAsync();
        Sut.AcceptUnscanned(Id);

        Assert.Null(Sut.AcceptUnscanned(Id));
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(Stored(UploadId), TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Only a file whose scan could not be finished can be kept unscanned: an upload still being scanned, one never completed, or one
    /// refused as infected is refused, and its staging file is left alone.
    /// </summary>
    [Fact]
    public async Task AcceptUnscanned_RefusesAnUploadThatAwaitsNoDecision()
    {
        await AppendAsync(0, 1, 2, 3);
        Assert.Equal(FileStorageRefusals.NotUndecided, Sut.AcceptUnscanned(Id));

        await CompleteAsync(3);
        Assert.Equal(FileStorageRefusals.NotUndecided, Sut.AcceptUnscanned(Id));

        await _queue.DequeueAsync(TestContext.Current.CancellationToken);
        _queue.Finish(Id, FileStorageRefusals.Infected);
        Assert.Equal(FileStorageRefusals.NotUndecided, Sut.AcceptUnscanned(Id));

        Assert.True(File.Exists(Staging(UploadId)));
        Assert.False(File.Exists(Stored(UploadId)));
    }

    /// <summary>After the owner said no (an abort), the file can no longer be kept.</summary>
    [Fact]
    public async Task AcceptUnscanned_RefusesAnAbortedUpload()
    {
        await UndecidedAsync();
        Sut.Abort(Id);

        Assert.Equal(FileStorageRefusals.NotUndecided, Sut.AcceptUnscanned(Id));
        Assert.False(File.Exists(Stored(UploadId)));
    }

    /// <summary>A decision whose staging file has gone (deleted by hand) is refused as an invalid upload.</summary>
    [Fact]
    public async Task AcceptUnscanned_RefusesWhenTheStagingFileIsGone()
    {
        await UndecidedAsync();
        File.Delete(Staging(UploadId));

        Assert.Equal(FileStorageRefusals.InvalidUpload, Sut.AcceptUnscanned(Id));
        Assert.False(File.Exists(Stored(UploadId)));
    }

    /// <summary>An id that is not a GUID is refused before anything touches the disk.</summary>
    [Fact]
    public void AcceptUnscanned_RefusesAnUploadIdThatIsNotAGuid()
    {
        Assert.Equal(FileStorageRefusals.InvalidUpload, Sut.AcceptUnscanned("../x"));
        Assert.Equal(FileStorageRefusals.InvalidUpload, Sut.AcceptUnscanned(null));
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
    /// An abort while the file is scanning cancels the scan; an abort after the scan stored it (the upload was never confirmed, D1 / D3)
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
