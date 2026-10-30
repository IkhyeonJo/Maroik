using System.Data;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging;
using H = Maroik.Core.Service.Tests.Fixtures.FileManagerServiceHarness;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// The upload flow of <see cref="FileManagerService"/>: the quota check before a batch, starting one file (name, size, name conflict
/// included, quota reserved under the account lock, abandoned uploads cleaned up), its chunks and scan polls — each recorded as the
/// upload's activity — its completion — scanned and stored
/// by file storage, an overwrite replacing the old file, an infected or unscannable file refused and logged — and an abort.
/// </summary>
public class FileManagerServiceUploadTests
{
    /// <summary>The service, its collaborators and the seeding helpers.</summary>
    private readonly H _h = new();

    /// <summary>A request to upload <paramref name="name"/> of <paramref name="size"/> bytes into <paramref name="parentId"/>.</summary>
    private static FileManagerUploadRequest Upload(string name, long size = 100, long? parentId = null, FileManagerConflictChoice conflict = FileManagerConflictChoice.Ask) =>
        new() { Name = name, Size = size, ParentId = parentId, Conflict = conflict };

    /// <summary>Starts an upload that must succeed and returns its start.</summary>
    private async Task<FileManagerUploadStart> StartAsync(FileManagerUploadRequest request)
    {
        var (result, start) = await _h.Sut.BeginUploadAsync(H.Email, request, TestContext.Current.CancellationToken);
        Assert.True(result.Success, result.ErrorKey);
        return start!;
    }

    // -- CheckQuotaAsync -----------------------------------------------------------

    /// <summary>A batch that fits the remaining quota is accepted; one that does not is refused with the shortfall in GB.</summary>
    [Fact]
    public async Task CheckQuotaAsync_ComparesTheBatch_WithTheRemainingQuota()
    {
        _h.QuotaBytes = 3 * H.Gb;
        _h.File("big1.bin", size: H.Gb);
        _h.File("big2.bin", size: H.Gb);

        ServiceResult fits = await _h.Sut.CheckQuotaAsync(H.Email, H.Gb, TestContext.Current.CancellationToken);
        ServiceResult tooBig = await _h.Sut.CheckQuotaAsync(H.Email, 2 * H.Gb, TestContext.Current.CancellationToken);

        Assert.True(fits.Success);
        Assert.Equal("FileManager.QuotaExceeded", tooBig.ErrorCode);
        Assert.Equal("Not enough storage: {0} GB short.", tooBig.ErrorKey);
        Assert.Equal([1.0m], tooBig.ErrorArgs);
    }

    /// <summary>
    /// An upload idle for more than an hour no longer counts, as in <see cref="FileManagerService.BeginUploadAsync"/>: otherwise an
    /// account it filled could never reach the upload that removes it. An upload still active keeps its reservation, however old.
    /// </summary>
    [Fact]
    public async Task CheckQuotaAsync_IgnoresStaleUploads_ButCountsActiveOnes()
    {
        _h.QuotaBytes = 2 * H.Gb;
        _h.File("idle.iso", size: H.Gb, uploading: true, created: H.Now.AddMinutes(-61));
        _h.File("active.iso", size: H.Gb, uploading: true, created: H.Now.AddMinutes(-59));

        ServiceResult fits = await _h.Sut.CheckQuotaAsync(H.Email, H.Gb, TestContext.Current.CancellationToken);
        ServiceResult tooBig = await _h.Sut.CheckQuotaAsync(H.Email, 2 * H.Gb, TestContext.Current.CancellationToken);

        Assert.True(fits.Success, fits.ErrorKey);
        Assert.Equal("FileManager.QuotaExceeded", tooBig.ErrorCode);
        Assert.Equal([1.0m], tooBig.ErrorArgs);
    }

    // -- BeginUploadAsync ----------------------------------------------------------

    /// <summary>
    /// Starting an upload locks the account, creates an "uploading" file stored under a new GUID (its size reserved) and answers the
    /// upload id, the name and the 16 MB chunk size.
    /// </summary>
    [Fact]
    public async Task BeginUploadAsync_ReservesTheFile_AndAnswersTheUploadId()
    {
        long docs = _h.Folder("Documents");

        FileManagerUploadStart start = await StartAsync(Upload("photo.png", 1234, docs));

        Assert.False(start.Conflict);
        Assert.Equal("photo.png", start.Name);
        Assert.Equal(FileManagerUploadPolicy.ChunkBytes, start.ChunkBytes);
        FileManagerNode node = Assert.Single(_h.Nodes.All, n => n.Name == "photo.png");
        Assert.Equal((true, 1234L, (long?)docs, (Guid?)start.UploadId), (node.Uploading, node.Size, node.ParentId, node.StorageId));
        _h.AssertAccountLocked();
        _h.AssertCommitted();
    }

    /// <summary>With "ask", a finished file of the same name stops the upload as a conflict for the user to decide; nothing is reserved.</summary>
    [Fact]
    public async Task BeginUploadAsync_WithAsk_ReportsANameConflict()
    {
        _h.File("a.txt");

        var (result, start) = await _h.Sut.BeginUploadAsync(H.Email, Upload("a.txt"), TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.True(start!.Conflict);
        Assert.Equal(Guid.Empty, start.UploadId);
        Assert.Single(_h.Nodes.All);
        _h.AssertRolledBack();
    }

    /// <summary>"Keep both" uploads under the first free "name (n)" (unfinished uploads count as taken too).</summary>
    [Fact]
    public async Task BeginUploadAsync_WithKeepBoth_PicksTheNextFreeName()
    {
        _h.File("a.txt");
        _h.File("a (1).txt", uploading: true);

        FileManagerUploadStart start = await StartAsync(Upload("a.txt", conflict: FileManagerConflictChoice.KeepBoth));

        Assert.Equal("a (2).txt", start.Name);
    }

    /// <summary>"Overwrite" starts the upload under the same name; the old file stays until the new one is stored.</summary>
    [Fact]
    public async Task BeginUploadAsync_WithOverwrite_KeepsTheName()
    {
        long old = _h.File("a.txt");

        FileManagerUploadStart start = await StartAsync(Upload("a.txt", conflict: FileManagerConflictChoice.Overwrite));

        Assert.Equal("a.txt", start.Name);
        Assert.NotNull(_h.Nodes.Get(old));
    }

    /// <summary>An upload over the quota is refused with the shortfall and logged at Warning (an audit event); nothing is reserved.</summary>
    [Fact]
    public async Task BeginUploadAsync_RefusesAnUploadOverTheQuota_AndLogsIt()
    {
        _h.QuotaBytes = H.Gb;
        _h.File("half.bin", size: H.Gb / 2, uploading: true);

        var (result, start) = await _h.Sut.BeginUploadAsync(H.Email, Upload("big.bin", H.Gb), TestContext.Current.CancellationToken);

        Assert.Equal("FileManager.QuotaExceeded", result.ErrorCode);
        Assert.Equal([0.5m], result.ErrorArgs);
        Assert.Null(start);
        Assert.Single(_h.Nodes.All);
        _h.AssertRolledBack();
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning
            && r.Message == $"File Manager upload refused for {H.Email}: over quota ({H.Gb} bytes requested, {H.Gb / 2} used of {H.Gb})");
    }

    /// <summary>Under the default quota of 0 GB nothing can be uploaded.</summary>
    [Fact]
    public async Task BeginUploadAsync_UnderTheDefaultZeroQuota_RefusesEveryUpload()
    {
        _h.QuotaBytes = 0;

        var (result, _) = await _h.Sut.BeginUploadAsync(H.Email, Upload("a.txt", 1), TestContext.Current.CancellationToken);

        Assert.Equal("FileManager.QuotaExceeded", result.ErrorCode);
    }

    /// <summary>
    /// An upload idle for more than an hour is removed when the same user starts a new one: its row is deleted inside the
    /// transaction — so it no longer counts against the quota — and its staging file is aborted after the commit. One active within the
    /// hour stays.
    /// </summary>
    [Fact]
    public async Task BeginUploadAsync_CleansUpStaleUploads_FirstFreeingTheirQuota()
    {
        _h.QuotaBytes = H.Gb;
        long stale = _h.File("old.iso", size: H.Gb, uploading: true, created: H.Now.AddMinutes(-61));
        long fresh = _h.File("new.iso", size: 1, uploading: true, created: H.Now.AddMinutes(-59));
        Guid staleId = _h.Node(stale).StorageId!.Value;

        FileManagerUploadStart start = await StartAsync(Upload("a.txt", H.Gb - 1));

        Assert.Null(_h.Nodes.Get(stale));
        Assert.NotNull(_h.Nodes.Get(fresh));
        Assert.NotEqual(Guid.Empty, start.UploadId);
        Assert.Single(_h.Files.AbortFileManagerUploadAsyncCalls, call => call.UploadId == staleId && call.FileStorageBaseUrl == H.BaseUrl);
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information && r.Message == $"File Manager: removed 1 abandoned upload(s) of {H.Email}");
    }

    /// <summary>An invalid name, a file too large to scan (the account may not keep it) and a folder the account does not own are refused.</summary>
    [Fact]
    public async Task BeginUploadAsync_RefusesAnInvalidNameSizeOrFolder()
    {
        long theirs = _h.Folder("Theirs", email: H.OtherEmail);

        Assert.Equal("FileManager.NameReserved", (await _h.Sut.BeginUploadAsync(H.Email, Upload(".."), TestContext.Current.CancellationToken)).Result.ErrorCode);
        Assert.Equal("FileManager.FileTooLarge", (await _h.Sut.BeginUploadAsync(H.Email, Upload("big.iso", FileManagerUploadPolicy.MaxScannableBytes + 1), TestContext.Current.CancellationToken)).Result.ErrorCode);
        Assert.Equal(ServiceErrorKeys.FileManagerItemNotFound, (await _h.Sut.BeginUploadAsync(H.Email, Upload("a.txt", parentId: theirs), TestContext.Current.CancellationToken)).Result.ErrorKey);
    }

    /// <summary>
    /// There is no upper size limit for an account whose virus scan is off: a file larger than ClamAV can scan is started (it will be
    /// stored without a scan). The setting is read from the locked account row, so an administrator's change made just before is the
    /// one that counts.
    /// </summary>
    [Theory]
    [InlineData(true, null)]
    [InlineData(false, true)]
    public async Task BeginUploadAsync_StartsAFileTooLargeToScan_WhenTheAccountsScanIsOff(bool scanDisabled, bool? scanDisabledUnderLock)
    {
        const long Size = 50L * H.Gb;
        _h.QuotaBytes = 100 * H.Gb;
        _h.ScanDisabled = scanDisabled;
        _h.ScanDisabledUnderLock = scanDisabledUnderLock;

        FileManagerUploadStart start = await StartAsync(Upload("backup.iso", Size));

        Assert.Equal(Size, Assert.Single(_h.Nodes.All, n => n.StorageId == start.UploadId).Size);
    }

    /// <summary>With the scan on (as the locked row sees it) such a file is refused before anything is stored.</summary>
    [Fact]
    public async Task BeginUploadAsync_RefusesAFileTooLargeToScan_WhenTheLockedAccountsScanIsOn()
    {
        _h.QuotaBytes = 100 * H.Gb;
        _h.ScanDisabled = true;
        _h.ScanDisabledUnderLock = false;

        var (result, start) = await _h.Sut.BeginUploadAsync(H.Email, Upload("backup.iso", 50L * H.Gb), TestContext.Current.CancellationToken);

        Assert.Equal("FileManager.FileTooLarge", result.ErrorCode);
        Assert.Null(start);
        Assert.Empty(_h.Nodes.All);
        Assert.Single(_h.UnitOfWork.RollbackAsyncCalls);
    }

    // -- AppendChunkAsync ----------------------------------------------------------

    /// <summary>A chunk of the user's upload is forwarded to file storage and the received length answered.</summary>
    [Fact]
    public async Task AppendChunkAsync_ForwardsTheChunk()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));

        var (result, length) = await _h.Sut.AppendChunkAsync(H.Email, start.UploadId, 0, new MemoryStream(new byte[4]), 4, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(4, length);
        Assert.Single(_h.Files.AppendFileManagerChunkAsyncCalls, call => call.UploadId == start.UploadId && call.Offset == 0 && call.FileStorageBaseUrl == H.BaseUrl);
    }

    /// <summary>A stored chunk is the upload's activity: its last activity becomes now, so a slow upload that keeps sending is never stale.</summary>
    [Fact]
    public async Task AppendChunkAsync_RecordsTheUploadsActivity()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Time.Advance(TimeSpan.FromMinutes(50));

        await _h.Sut.AppendChunkAsync(H.Email, start.UploadId, 0, new MemoryStream(new byte[4]), 4, TestContext.Current.CancellationToken);

        Assert.Equal(H.Now.AddMinutes(50), Assert.Single(_h.Nodes.All).Updated);
    }

    /// <summary>A chunk file storage did not store is no activity: the upload's last activity stays.</summary>
    [Fact]
    public async Task AppendChunkAsync_RecordsNoActivity_WhenFileStorageRefuses()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Files.AnswerAppendFileManagerChunkAsync((_, _, _, fileStorageBaseUrl, _) => fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => Task.FromResult<long?>(null));
        _h.Time.Advance(TimeSpan.FromMinutes(50));

        await _h.Sut.AppendChunkAsync(H.Email, start.UploadId, 0, new MemoryStream([1]), 1, TestContext.Current.CancellationToken);

        Assert.Equal(H.Now, Assert.Single(_h.Nodes.All).Updated);
    }

    /// <summary>A chunk that would go past the declared size is refused before it reaches file storage.</summary>
    [Fact]
    public async Task AppendChunkAsync_RefusesAChunkPastTheDeclaredSize()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));

        var (result, _) = await _h.Sut.AppendChunkAsync(H.Email, start.UploadId, 8, new MemoryStream(new byte[4]), 4, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(ServiceErrorKeys.InputInvalid, result.ErrorKey);
        Assert.Empty(_h.Files.AppendFileManagerChunkAsyncCalls);
    }

    /// <summary>An upload id the account does not have (another account's, a finished file's) is "not found".</summary>
    [Fact]
    public async Task AppendChunkAsync_AnswersNotFound_ForAnUploadTheAccountDoesNotHave()
    {
        long finished = _h.File("done.txt");
        long theirs = _h.File("x.bin", uploading: true, email: H.OtherEmail);

        foreach (Guid id in new[] { _h.Node(finished).StorageId!.Value, _h.Node(theirs).StorageId!.Value, Guid.NewGuid() })
            Assert.Equal(ServiceErrorKeys.FileManagerUploadNotFound, (await _h.Sut.AppendChunkAsync(H.Email, id, 0, new MemoryStream([1]), 1, TestContext.Current.CancellationToken)).Result.ErrorKey);
    }

    /// <summary>A chunk file storage refuses (or cannot take) is a temporary error, logged at Warning.</summary>
    [Fact]
    public async Task AppendChunkAsync_AnswersATemporaryError_WhenFileStorageRefuses()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Files.AnswerAppendFileManagerChunkAsync((_, _, _, fileStorageBaseUrl, _) => fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => Task.FromResult<long?>(null));

        var (result, _) = await _h.Sut.AppendChunkAsync(H.Email, start.UploadId, 0, new MemoryStream([1]), 1, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Failure, result.ErrorType);
        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager: chunk at 0 of upload {start.UploadId} of {H.Email} was not stored");
    }

    // -- CompleteUploadAsync -------------------------------------------------------

    /// <summary>A stored upload becomes a normal file of the drive (under the account lock, in a transaction).</summary>
    [Fact]
    public async Task CompleteUploadAsync_MakesTheFileANormalItem()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.UnitOfWork.ClearCalls();
        _h.Accounts.ClearCalls();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, overwrite: false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.False(Assert.Single(_h.Nodes.All).Uploading);
        Assert.Single(_h.Files.CompleteFileManagerUploadAsyncCalls, call => call.UploadId == start.UploadId && call.ExpectedSize == 10 && call.SkipScan == false && call.FileStorageBaseUrl == H.BaseUrl);
        Assert.DoesNotContain(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning);
        _h.AssertAccountLocked();
        _h.AssertCommitted();
    }

    /// <summary>An overwrite deletes the old file's row in the transaction and its stored file after the commit, logged as an audit event.</summary>
    [Fact]
    public async Task CompleteUploadAsync_WithOverwrite_ReplacesTheOldFile()
    {
        long old = _h.File("a.txt");
        string oldPath = _h.Node(old).StoragePath!;
        FileManagerUploadStart start = await StartAsync(Upload("a.txt", conflict: FileManagerConflictChoice.Overwrite));

        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, overwrite: true, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Null(_h.Nodes.Get(old));
        Assert.False(Assert.Single(_h.Nodes.All).Uploading);
        Assert.Single(_h.Files.DeleteFileManagerFileAsyncCalls, call => call.StoragePath == oldPath && call.FileStorageBaseUrl == H.BaseUrl);
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information && r.Message == $"File Manager: {H.Email} overwrote file {old}");
    }

    /// <summary>
    /// Without an overwrite, a finished file of the same name that appeared meanwhile makes the completion fail as a conflict: the new
    /// upload's row and stored file are removed and the old file stays.
    /// </summary>
    [Fact]
    public async Task CompleteUploadAsync_WithoutOverwrite_FailsOnANameThatWasTakenMeanwhile()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.txt"));
        long meanwhile = _h.File("a.txt");
        string newPath = $"upload/FileManager/{start.UploadId:D}";

        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, overwrite: false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileManagerNameTaken, result.ErrorKey);
        Assert.Equal([meanwhile], _h.Nodes.All.Select(n => n.Id));
        Assert.Single(_h.Files.DeleteFileManagerFileAsyncCalls, call => call.StoragePath == newPath && call.FileStorageBaseUrl == H.BaseUrl);
    }

    /// <summary>An infected or unscannable file is not kept: its row is deleted, the refusal says which, and it is logged at Warning.</summary>
    [Theory]
    [InlineData(FileUploadResult.Infected, ServiceErrorKeys.FileManagerInfected, "infected")]
    [InlineData(FileUploadResult.ScanUnavailable, ServiceErrorKeys.FileManagerScanUnavailable, "could not be scanned")]
    public async Task CompleteUploadAsync_RefusesAnInfectedOrUnscannableFile(FileUploadResult outcome, string key, string reason)
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Files.AnswerCompleteFileManagerUploadAsync((uploadId, expectedSize, _, fileStorageBaseUrl, _) => uploadId == start.UploadId && expectedSize == 10 && fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => Task.FromResult<FileUploadResult>(outcome));

        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(key, result.ErrorKey);
        Assert.Empty(_h.Nodes.All);
        long id = 1;
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager upload refused for {H.Email}: file {id} {reason}");
    }

    /// <summary>A storage failure is a temporary error; the reservation is released.</summary>
    [Fact]
    public async Task CompleteUploadAsync_AnswersATemporaryError_WhenStorageFails()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Files.AnswerCompleteFileManagerUploadAsync((uploadId, expectedSize, _, fileStorageBaseUrl, _) => uploadId == start.UploadId && expectedSize == 10 && fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Failed));

        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.Empty(_h.Nodes.All);
    }

    /// <summary>
    /// While file storage is still scanning, the answer is "scanning" and nothing changes: the reservation stays, nothing is locked,
    /// committed or deleted (the page asks again).
    /// </summary>
    [Fact]
    public async Task CompleteUploadAsync_WhileTheScanRuns_AnswersScanning_AndChangesNothing()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Files.AnswerCompleteFileManagerUploadAsync((uploadId, expectedSize, _, fileStorageBaseUrl, _) => uploadId == start.UploadId && expectedSize == 10 && fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Scanning));
        _h.UnitOfWork.ClearCalls();
        _h.Accounts.ClearCalls();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Scanning, completion);
        Assert.True(Assert.Single(_h.Nodes.All).Uploading);
        Assert.Empty(_h.UnitOfWork.BeginAsyncCalls);
        Assert.Empty(_h.Accounts.FindByEmailForUpdateAsyncCalls);
        Assert.Empty(_h.Files.DeleteFileManagerFileAsyncCalls);
    }

    /// <summary>
    /// Each poll while the scan runs is the upload's activity: a long scan whose page keeps asking is never stale, while one whose
    /// page was closed stops being touched and is swept an hour later.
    /// </summary>
    [Fact]
    public async Task CompleteUploadAsync_WhileTheScanRuns_RecordsTheUploadsActivity()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Files.AnswerCompleteFileManagerUploadAsync((uploadId, expectedSize, _, fileStorageBaseUrl, _) => uploadId == start.UploadId && expectedSize == 10 && fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Scanning));
        _h.Time.Advance(TimeSpan.FromMinutes(50));

        await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(H.Now.AddMinutes(50), Assert.Single(_h.Nodes.All).Updated);
    }

    /// <summary>An upload that is already completed (a repeated answer, a second tab) is "stored" again; file storage is not asked.</summary>
    [Fact]
    public async Task CompleteUploadAsync_OfACompletedUpload_AnswersStoredAgain()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);
        _h.Files.ClearCalls();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.False(Assert.Single(_h.Nodes.All).Uploading);
        Assert.Empty(_h.Files.CompleteFileManagerUploadAsyncCalls);
    }

    /// <summary>
    /// Two answers for one upload at once: the one that finds it already completed under the lock answers "stored" too, and neither
    /// deletes the stored file nor writes again.
    /// </summary>
    [Fact]
    public async Task CompleteUploadAsync_CompletedMeanwhile_AnswersStored_AndKeepsTheFile()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        FileManagerNode completedByTheOther = Assert.Single(_h.Nodes.All);
        completedByTheOther.CompleteUpload(H.Now);
        _h.Files.AnswerCompleteFileManagerUploadAsync((uploadId, expectedSize, _, fileStorageBaseUrl, _) => uploadId == start.UploadId && expectedSize == 10 && fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => { _h.Nodes.UpdateEntityAsync(completedByTheOther, TestContext.Current.CancellationToken); return Task.FromResult<FileUploadResult>(FileUploadResult.Stored); });
        _h.UnitOfWork.ClearCalls();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.False(Assert.Single(_h.Nodes.All).Uploading);
        Assert.Empty(_h.Files.DeleteFileManagerFileAsyncCalls);
        Assert.Empty(_h.UnitOfWork.CommitAsyncCalls);
        Assert.Single(_h.UnitOfWork.RollbackAsyncCalls);
    }

    /// <summary>
    /// An upload removed while it was scanning (its folder was deleted) is "not found" under the lock: nothing is written, and the file
    /// the scan stored is deleted after the rollback.
    /// </summary>
    [Fact]
    public async Task CompleteUploadAsync_RemovedMeanwhile_DeletesTheStoredFile()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        long id = Assert.Single(_h.Nodes.All).Id;
        _h.Files.AnswerCompleteFileManagerUploadAsync((uploadId, expectedSize, _, fileStorageBaseUrl, _) => uploadId == start.UploadId && expectedSize == 10 && fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => { _h.Nodes.DeleteByEmailAndIdsAsync(H.Email, [id], TestContext.Current.CancellationToken); return Task.FromResult<FileUploadResult>(FileUploadResult.Stored); });
        _h.UnitOfWork.ClearCalls();

        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileManagerUploadNotFound, result.ErrorKey);
        Assert.Empty(_h.Nodes.All);
        Assert.Single(_h.UnitOfWork.RollbackAsyncCalls);
        Assert.Empty(_h.UnitOfWork.CommitAsyncCalls);
        Assert.Single(_h.Files.DeleteFileManagerFileAsyncCalls, call => call.StoragePath == $"upload/FileManager/{start.UploadId:D}" && call.FileStorageBaseUrl == H.BaseUrl);
    }

    /// <summary>An upload that is not the account's is "not found", and file storage is not asked.</summary>
    [Fact]
    public async Task CompleteUploadAsync_AnswersNotFound_ForAnUnknownUpload()
    {
        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, Guid.NewGuid(), false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileManagerUploadNotFound, result.ErrorKey);
        Assert.Empty(_h.Files.CompleteFileManagerUploadAsyncCalls);
    }

    // -- A file the scan could not finish / the scan turned off --------------------

    /// <summary>
    /// A file the scan could not finish (too large for the scanner's limits, or the scanner unavailable) is refused ("could not be
    /// scanned"): its row is deleted and the refusal is logged. File storage already dropped the file, so nothing is aborted. This holds
    /// for an account whose scan is off too (its scan was queued before the administrator turned it off).
    /// </summary>
    [Theory]
    [InlineData(FileUploadResult.ScanLimitExceeded, false)]
    [InlineData(FileUploadResult.ScanUnavailable, false)]
    [InlineData(FileUploadResult.ScanLimitExceeded, true)]
    public async Task CompleteUploadAsync_RefusesAFileTheScanCouldNotFinish(FileUploadResult outcome, bool scanDisabled)
    {
        _h.ScanDisabled = scanDisabled;
        FileManagerUploadStart start = await StartAsync(Upload("big.zip", 10));
        _h.Files.AnswerCompleteFileManagerUploadAsync((uploadId, expectedSize, _, fileStorageBaseUrl, _) => uploadId == start.UploadId && expectedSize == 10 && fileStorageBaseUrl == H.BaseUrl, (_, _, _, _, _) => Task.FromResult<FileUploadResult>(outcome));

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.Equal(("FileManager.ScanUnavailable", ServiceErrorKeys.FileManagerScanUnavailable), (result.ErrorCode, result.ErrorKey));
        Assert.Empty(_h.Nodes.All);
        Assert.Empty(_h.Files.AbortFileManagerUploadAsyncCalls);
        Assert.Empty(_h.Files.DeleteFileManagerFileAsyncCalls);
        long id = 1;
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager upload refused for {H.Email}: file {id} could not be scanned");
    }

    /// <summary>
    /// An account whose virus scan an administrator turned off: file storage is told to skip the scan, the stored file becomes a normal
    /// item at once (account locked, in a transaction), and the store is an audit event logged at Warning with the account, the file and
    /// its size.
    /// </summary>
    [Fact]
    public async Task CompleteUploadAsync_SkipsTheScan_WhenTheAccountsScanIsOff()
    {
        _h.ScanDisabled = true;
        FileManagerUploadStart start = await StartAsync(Upload("big.zip", 10));
        _h.UnitOfWork.ClearCalls();
        _h.Accounts.ClearCalls();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.ErrorKey);
        Assert.Equal(FileManagerCompletion.Decided, completion);
        FileManagerNode file = Assert.Single(_h.Nodes.All);
        Assert.False(file.Uploading);
        Assert.Single(_h.Files.CompleteFileManagerUploadAsyncCalls, call => call.UploadId == start.UploadId && call.ExpectedSize == 10 && call.SkipScan == true && call.FileStorageBaseUrl == H.BaseUrl);
        _h.AssertAccountLocked();
        _h.AssertCommitted();
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning
            && r.Message == $"File Manager: {H.Email} stored file {file.Id} (10 bytes) with the virus scan turned off");
    }

    /// <summary>The scan's setting is read when the upload completes: an administrator's change after it started is the one that counts.</summary>
    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task CompleteUploadAsync_ReadsTheScanSetting_WhenTheUploadCompletes(bool atStart, bool atCompletion)
    {
        _h.ScanDisabled = atStart;
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.ScanDisabled = atCompletion;

        await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Single(_h.Files.CompleteFileManagerUploadAsyncCalls, call => call.UploadId == start.UploadId && call.ExpectedSize == 10 && call.SkipScan == atCompletion && call.FileStorageBaseUrl == H.BaseUrl);
    }

    // -- AbortUploadAsync ----------------------------------------------------------

    /// <summary>An abort deletes the reservation and drops what file storage received.</summary>
    [Fact]
    public async Task AbortUploadAsync_ReleasesTheUpload()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));

        ServiceResult result = await _h.Sut.AbortUploadAsync(H.Email, start.UploadId, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Empty(_h.Nodes.All);
        Assert.Single(_h.Files.AbortFileManagerUploadAsyncCalls, call => call.UploadId == start.UploadId && call.FileStorageBaseUrl == H.BaseUrl);
    }

    /// <summary>A finished file cannot be "aborted": that upload id is not found and the file stays.</summary>
    [Fact]
    public async Task AbortUploadAsync_AnswersNotFound_ForAFinishedFile()
    {
        long done = _h.File("a.txt");

        Assert.Equal(ServiceErrorKeys.FileManagerUploadNotFound, (await _h.Sut.AbortUploadAsync(H.Email, _h.Node(done).StorageId!.Value, TestContext.Current.CancellationToken)).ErrorKey);
        Assert.NotNull(_h.Nodes.Get(done));
    }
}
