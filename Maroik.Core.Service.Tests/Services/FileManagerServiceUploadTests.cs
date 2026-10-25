using System.Data;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;
using H = Maroik.Core.Service.Tests.Fixtures.FileManagerServiceHarness;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// The upload flow of <see cref="FileManagerService"/>: the quota check before a batch (F2), starting one file (name, size, name conflict
/// F2-C, quota reserved under the account lock Q4, abandoned uploads cleaned up Q5), its chunks (P5), its completion — scanned and stored
/// by file storage, an overwrite replacing the old file, an infected or unscannable file refused and logged (S2, S8) — and an abort.
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

    /// <summary>An upload over the quota is refused with the shortfall and logged at Warning (an audit event, S8); nothing is reserved.</summary>
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
    /// An upload left unfinished for more than 24 hours is removed when the same user starts a new one (Q5): its row is deleted inside
    /// the transaction — so it no longer counts against the quota — and its staging file is aborted after the commit.
    /// </summary>
    [Fact]
    public async Task BeginUploadAsync_CleansUpStaleUploads_FirstFreeingTheirQuota()
    {
        _h.QuotaBytes = H.Gb;
        long stale = _h.File("old.iso", size: H.Gb, uploading: true, created: H.Now.AddHours(-25));
        long fresh = _h.File("new.iso", size: 1, uploading: true, created: H.Now.AddHours(-1));
        Guid staleId = _h.Node(stale).StorageId!.Value;

        FileManagerUploadStart start = await StartAsync(Upload("a.txt", H.Gb - 1));

        Assert.Null(_h.Nodes.Get(stale));
        Assert.NotNull(_h.Nodes.Get(fresh));
        Assert.NotEqual(Guid.Empty, start.UploadId);
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(staleId, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information && r.Message == $"File Manager: removed 1 abandoned upload(s) of {H.Email}");
    }

    /// <summary>An invalid name, a file over 2 GB and a folder the account does not own are refused.</summary>
    [Fact]
    public async Task BeginUploadAsync_RefusesAnInvalidNameSizeOrFolder()
    {
        long theirs = _h.Folder("Theirs", email: H.OtherEmail);

        Assert.Equal("FileManager.NameReserved", (await _h.Sut.BeginUploadAsync(H.Email, Upload(".."), TestContext.Current.CancellationToken)).Result.ErrorCode);
        Assert.Equal("FileManager.FileTooLarge", (await _h.Sut.BeginUploadAsync(H.Email, Upload("big.iso", FileManagerUploadPolicy.MaxFileBytes + 1), TestContext.Current.CancellationToken)).Result.ErrorCode);
        Assert.Equal(ServiceErrorKeys.FileManagerItemNotFound, (await _h.Sut.BeginUploadAsync(H.Email, Upload("a.txt", parentId: theirs), TestContext.Current.CancellationToken)).Result.ErrorKey);
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
        _h.Files.Verify(f => f.AppendFileManagerChunkAsync(start.UploadId, 0, It.IsAny<Stream>(), H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>A chunk that would go past the declared size is refused before it reaches file storage.</summary>
    [Fact]
    public async Task AppendChunkAsync_RefusesAChunkPastTheDeclaredSize()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));

        var (result, _) = await _h.Sut.AppendChunkAsync(H.Email, start.UploadId, 8, new MemoryStream(new byte[4]), 4, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorType.Validation, result.ErrorType);
        Assert.Equal(ServiceErrorKeys.InputInvalid, result.ErrorKey);
        _h.Files.Verify(f => f.AppendFileManagerChunkAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _h.Files.Setup(f => f.AppendFileManagerChunkAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<Stream>(), H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync((long?)null);

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
        _h.UnitOfWork.Invocations.Clear();
        _h.Accounts.Invocations.Clear();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, overwrite: false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.False(Assert.Single(_h.Nodes.All).Uploading);
        _h.Files.Verify(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        _h.AssertAccountLocked();
        _h.AssertCommitted();
    }

    /// <summary>An overwrite deletes the old file's row in the transaction and its stored file after the commit, logged as an audit event (S8).</summary>
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
        _h.Files.Verify(f => f.DeleteFileManagerFileAsync(oldPath, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
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
        _h.Files.Verify(f => f.DeleteFileManagerFileAsync(newPath, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>An infected or unscannable file is not kept: its row is deleted, the refusal says which, and it is logged at Warning (S8).</summary>
    [Theory]
    [InlineData(FileUploadResult.Infected, ServiceErrorKeys.FileManagerInfected, "infected")]
    [InlineData(FileUploadResult.ScanUnavailable, ServiceErrorKeys.FileManagerScanUnavailable, "could not be scanned")]
    public async Task CompleteUploadAsync_RefusesAnInfectedOrUnscannableFile(FileUploadResult outcome, string key, string reason)
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(outcome);

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
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(FileUploadResult.Failed);

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
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(FileUploadResult.Scanning);
        _h.UnitOfWork.Invocations.Clear();
        _h.Accounts.Invocations.Clear();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Scanning, completion);
        Assert.True(Assert.Single(_h.Nodes.All).Uploading);
        _h.UnitOfWork.Verify(u => u.BeginAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Never);
        _h.Accounts.Verify(a => a.FindByEmailForUpdateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _h.Files.Verify(f => f.DeleteFileManagerFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>An upload that is already completed (a repeated answer, a second tab) is "stored" again; file storage is not asked.</summary>
    [Fact]
    public async Task CompleteUploadAsync_OfACompletedUpload_AnswersStoredAgain()
    {
        FileManagerUploadStart start = await StartAsync(Upload("a.bin", 10));
        await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);
        _h.Files.Invocations.Clear();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.False(Assert.Single(_h.Nodes.All).Uploading);
        _h.Files.Verify(f => f.CompleteFileManagerUploadAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>()))
            .Callback(() => _h.Nodes.UpdateEntityAsync(completedByTheOther, TestContext.Current.CancellationToken))
            .ReturnsAsync(FileUploadResult.Stored);
        _h.UnitOfWork.Invocations.Clear();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.False(Assert.Single(_h.Nodes.All).Uploading);
        _h.Files.Verify(f => f.DeleteFileManagerFileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _h.UnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _h.UnitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
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
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>()))
            .Callback(() => _h.Nodes.DeleteByEmailAndIdsAsync(H.Email, [id], TestContext.Current.CancellationToken))
            .ReturnsAsync(FileUploadResult.Stored);
        _h.UnitOfWork.Invocations.Clear();

        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileManagerUploadNotFound, result.ErrorKey);
        Assert.Empty(_h.Nodes.All);
        _h.UnitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _h.UnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _h.Files.Verify(f => f.DeleteFileManagerFileAsync($"upload/FileManager/{start.UploadId:D}", H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>An upload that is not the account's is "not found", and file storage is not asked.</summary>
    [Fact]
    public async Task CompleteUploadAsync_AnswersNotFound_ForAnUnknownUpload()
    {
        (ServiceResult result, _) = await _h.Sut.CompleteUploadAsync(H.Email, Guid.NewGuid(), false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileManagerUploadNotFound, result.ErrorKey);
        _h.Files.Verify(f => f.CompleteFileManagerUploadAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -- A file the scan could not finish ------------------------------------------

    /// <summary>
    /// When the scan could not be finished (too large for the scanner's limits, or the scanner unavailable) and the account may keep
    /// such files, the owner is asked: the answer awaits their decision with the question for the reason and the file's name, nothing
    /// is written, deleted or aborted, and it is logged.
    /// </summary>
    [Theory]
    [InlineData(FileUploadResult.ScanLimitExceeded, ServiceErrorKeys.FileManagerConfirmUnscannedTooLarge)]
    [InlineData(FileUploadResult.ScanUnavailable, ServiceErrorKeys.FileManagerConfirmUnscannedUnavailable)]
    public async Task CompleteUploadAsync_AsksTheOwner_WhenTheScanCouldNotFinish_AndTheAccountMayKeepIt(FileUploadResult outcome, string question)
    {
        _h.AllowUnscanned = true;
        FileManagerUploadStart start = await StartAsync(Upload("big.zip", 10));
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(outcome);
        _h.UnitOfWork.Invocations.Clear();

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(FileManagerCompletion.AwaitingDecision, completion);
        Assert.Equal(("FileManager.ConfirmUnscanned", question, ServiceErrorType.Validation), (result.ErrorCode, result.ErrorKey, result.ErrorType));
        Assert.Equal(["big.zip"], result.ErrorArgs);
        Assert.True(Assert.Single(_h.Nodes.All).Uploading);
        _h.UnitOfWork.Verify(u => u.BeginAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Never);
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        long id = 1;
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information
            && r.Message == $"File Manager upload of file {id} of {H.Email} could not be scanned ({outcome}); its owner decides whether to keep it");
    }

    /// <summary>
    /// When the account may not keep such files, nothing is asked: the file is refused as before ("could not be scanned"), its row is
    /// deleted, what file storage kept for a decision is dropped (abort), and the refusal is logged.
    /// </summary>
    [Theory]
    [InlineData(FileUploadResult.ScanLimitExceeded)]
    [InlineData(FileUploadResult.ScanUnavailable)]
    public async Task CompleteUploadAsync_RefusesAFileTheScanCouldNotFinish_WhenTheAccountMayNotKeepIt(FileUploadResult outcome)
    {
        FileManagerUploadStart start = await StartAsync(Upload("big.zip", 10));
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(outcome);

        (ServiceResult result, FileManagerCompletion completion) = await _h.Sut.CompleteUploadAsync(H.Email, start.UploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(FileManagerCompletion.Decided, completion);
        Assert.Equal(("FileManager.ScanUnavailable", ServiceErrorKeys.FileManagerScanUnavailable), (result.ErrorCode, result.ErrorKey));
        Assert.Empty(_h.Nodes.All);
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(start.UploadId, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        long id = 1;
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager upload refused for {H.Email}: file {id} could not be scanned");
    }

    // -- AcceptUnscannedUploadAsync ------------------------------------------------

    /// <summary>Brings an upload of "big.zip" (10 bytes) to "waiting for its owner's decision" and returns its id.</summary>
    private async Task<Guid> UndecidedAsync(string name = "big.zip", FileManagerConflictChoice conflict = FileManagerConflictChoice.Ask)
    {
        FileManagerUploadStart start = await StartAsync(Upload(name, 10, conflict: conflict));
        _h.Files.Setup(f => f.CompleteFileManagerUploadAsync(start.UploadId, 10, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(FileUploadResult.ScanLimitExceeded);
        _h.UnitOfWork.Invocations.Clear();
        _h.Accounts.Invocations.Clear();
        return start.UploadId;
    }

    /// <summary>
    /// The owner keeps the file: file storage stores it unscanned, then (account locked, in a transaction) it becomes a normal file
    /// marked unscanned — an audit event logged at Warning with the account, the file and its size.
    /// </summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_StoresTheFile_MarkedUnscanned()
    {
        _h.AllowUnscanned = true;
        Guid uploadId = await UndecidedAsync();

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, overwrite: false, TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.ErrorKey);
        FileManagerNode file = Assert.Single(_h.Nodes.All);
        Assert.Equal((false, true), (file.Uploading, file.Unscanned));
        _h.Files.Verify(f => f.AcceptUnscannedFileManagerUploadAsync(uploadId, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        _h.AssertAccountLocked();
        _h.AssertCommitted();
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning
            && r.Message == $"File Manager: {H.Email} stored file {file.Id} (10 bytes) without a virus scan");
    }

    /// <summary>Kept with an overwrite, the unscanned file replaces the old one like any completed upload.</summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_WithOverwrite_ReplacesTheOldFile()
    {
        _h.AllowUnscanned = true;
        long old = _h.File("big.zip");
        string oldPath = _h.Node(old).StoragePath!;
        Guid uploadId = await UndecidedAsync(conflict: FileManagerConflictChoice.Overwrite);

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, overwrite: true, TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.ErrorKey);
        Assert.Null(_h.Nodes.Get(old));
        Assert.True(Assert.Single(_h.Nodes.All).Unscanned);
        _h.Files.Verify(f => f.DeleteFileManagerFileAsync(oldPath, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// An account an administrator does not allow it (a direct request) is refused before file storage stores anything: the upload is
    /// released (row deleted, file storage aborted) and the denial is logged at Warning.
    /// </summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_RefusesAnAccountThatMayNotKeepUnscannedFiles()
    {
        Guid uploadId = await UndecidedAsync();

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(("FileManager.UnscannedNotAllowed", ServiceErrorKeys.FileManagerUnscannedNotAllowed, ServiceErrorType.Validation), (result.ErrorCode, result.ErrorKey, result.ErrorType));
        Assert.Empty(_h.Nodes.All);
        _h.Files.Verify(f => f.AcceptUnscannedFileManagerUploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(uploadId, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        long id = 1;
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager: {H.Email} may not keep file {id} unscanned");
    }

    /// <summary>
    /// An administrator who disallows it while the owner decides wins: the check under the account lock refuses, the upload's row is
    /// deleted in the transaction and the file storage stored is deleted after the commit.
    /// </summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_RefusesUnderTheLock_WhenDisallowedMeanwhile()
    {
        _h.AllowUnscanned = true;
        _h.AllowUnscannedUnderLock = false;
        Guid uploadId = await UndecidedAsync();

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileManagerUnscannedNotAllowed, result.ErrorKey);
        Assert.Empty(_h.Nodes.All);
        _h.AssertCommitted();
        _h.Files.Verify(f => f.DeleteFileManagerFileAsync($"upload/FileManager/{uploadId:D}", H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        long id = 1;
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager: {H.Email} may not keep file {id} unscanned");
    }

    /// <summary>File storage refusing to keep it (no longer waiting, e.g. after its restart) is a temporary error, logged; nothing is written.</summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_AnswersATemporaryError_WhenFileStorageRefuses()
    {
        _h.AllowUnscanned = true;
        Guid uploadId = await UndecidedAsync();
        _h.Files.Setup(f => f.AcceptUnscannedFileManagerUploadAsync(uploadId, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        Assert.True(Assert.Single(_h.Nodes.All).Uploading);
        _h.UnitOfWork.Verify(u => u.BeginAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager: upload {uploadId} of {H.Email} could not be stored unscanned");
    }

    /// <summary>A failure inside the transaction is logged at Error with the exception, rolled back, and answered as a temporary error.</summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_RollsBack_AndLogs_WhenTheTransactionFails()
    {
        _h.AllowUnscanned = true;
        Guid uploadId = await UndecidedAsync();
        _h.UnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("db down"));

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceResult.TemporaryErrorKey, result.ErrorKey);
        _h.UnitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        FakeLogRecord error = Assert.Single(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Equal($"File Manager: failed to complete upload {uploadId} of {H.Email}", error.Message);
        Assert.Equal("db down", error.Exception?.Message);
    }

    /// <summary>An upload already completed (a repeated "yes") answers success again; file storage is not asked.</summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_OfACompletedUpload_AnswersSuccessAgain()
    {
        _h.AllowUnscanned = true;
        Guid uploadId = await UndecidedAsync();
        await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, false, TestContext.Current.CancellationToken);
        _h.Files.Invocations.Clear();

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, uploadId, false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _h.Files.Verify(f => f.AcceptUnscannedFileManagerUploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>An upload that is not the account's is "not found", and file storage is not asked.</summary>
    [Fact]
    public async Task AcceptUnscannedUploadAsync_AnswersNotFound_ForAnUnknownUpload()
    {
        _h.AllowUnscanned = true;

        ServiceResult result = await _h.Sut.AcceptUnscannedUploadAsync(H.Email, Guid.NewGuid(), false, TestContext.Current.CancellationToken);

        Assert.Equal(ServiceErrorKeys.FileManagerUploadNotFound, result.ErrorKey);
        _h.Files.Verify(f => f.AcceptUnscannedFileManagerUploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
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
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(start.UploadId, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
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
