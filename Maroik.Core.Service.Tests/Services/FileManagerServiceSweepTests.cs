using System.Data;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging;
using Moq;
using H = Maroik.Core.Service.Tests.Fixtures.FileManagerServiceHarness;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// The worker's hourly sweep of abandoned uploads, <see cref="FileManagerService.RemoveStaleUploadsAsync"/> (Q5): an upload idle for
/// more than an hour (no chunk, no scan poll) is removed — its row inside a transaction that first locks the account, its file in file
/// storage after the commit — account by account, so one account's failure leaves the others to be swept.
/// </summary>
public class FileManagerServiceSweepTests
{
    /// <summary>The service, its collaborators and the seeding helpers.</summary>
    private readonly H _h = new();

    /// <summary>Runs the sweep over at most <paramref name="maxAccounts"/> accounts.</summary>
    private Task<int> SweepAsync(int maxAccounts = 100) => _h.Sut.RemoveStaleUploadsAsync(maxAccounts, TestContext.Current.CancellationToken);

    /// <summary>
    /// The idle uploads of every account are removed and aborted in file storage after the commit, one logged line per account; an upload
    /// active within the hour (however long ago it started), a finished file and a folder stay.
    /// </summary>
    [Fact]
    public async Task RemoveStaleUploadsAsync_RemovesTheIdleUploads_OfEveryAccount()
    {
        List<string> steps = [];
        _h.UnitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Callback(() => steps.Add("commit")).Returns(Task.CompletedTask);
        _h.Files.Setup(f => f.AbortFileManagerUploadAsync(It.IsAny<Guid>(), H.BaseUrl, It.IsAny<CancellationToken>())).Callback(() => steps.Add("abort")).ReturnsAsync(true);
        long idle = _h.File("idle.iso", size: H.Gb, uploading: true, created: H.Now.AddMinutes(-61));
        long theirs = _h.File("theirs.iso", uploading: true, email: H.OtherEmail, created: H.Now.AddDays(-3));
        long active = _h.File("active.iso", uploading: true, created: H.Now.AddMinutes(-59));
        long finished = _h.File("done.txt", created: H.Now.AddDays(-30));
        long folder = _h.Folder("Docs");
        Guid idleId = _h.Node(idle).StorageId!.Value;
        Guid theirsId = _h.Node(theirs).StorageId!.Value;

        int removed = await SweepAsync();

        Assert.Equal(2, removed);
        Assert.Equal([active, finished, folder], _h.Nodes.All.Select(n => n.Id).Order());
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(idleId, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(theirsId, H.BaseUrl, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(["commit", "abort", "commit", "abort"], steps);
        _h.Accounts.Verify(a => a.FindByEmailForUpdateAsync(H.Email, It.IsAny<CancellationToken>()), Times.Once);
        _h.Accounts.Verify(a => a.FindByEmailForUpdateAsync(H.OtherEmail, It.IsAny<CancellationToken>()), Times.Once);
        _h.UnitOfWork.Verify(u => u.BeginAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _h.UnitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
        string[] logged = [.. _h.Logger.Collector.GetSnapshot().Where(r => r.Level == LogLevel.Information).Select(r => r.Message)];
        Assert.Equal([$"File Manager: removed 1 abandoned upload(s) of {H.OtherEmail}", $"File Manager: removed 1 abandoned upload(s) of {H.Email}"], logged);
    }

    /// <summary>With nothing idle the sweep removes nothing and opens no transaction.</summary>
    [Fact]
    public async Task RemoveStaleUploadsAsync_DoesNothing_WithoutAnIdleUpload()
    {
        _h.File("active.iso", uploading: true, created: H.Now.AddMinutes(-30));

        Assert.Equal(0, await SweepAsync());

        Assert.Single(_h.Nodes.All);
        _h.UnitOfWork.Verify(u => u.BeginAsync(It.IsAny<IsolationLevel?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>At most <c>maxAccounts</c> accounts are swept (in email order); the rest wait for the next sweep.</summary>
    [Fact]
    public async Task RemoveStaleUploadsAsync_SweepsAtMostTheGivenNumberOfAccounts()
    {
        long mine = _h.File("mine.iso", uploading: true, created: H.Now.AddHours(-2));
        _h.File("theirs.iso", uploading: true, email: H.OtherEmail, created: H.Now.AddHours(-2));

        Assert.Equal(1, await SweepAsync(maxAccounts: 1));

        Assert.Equal([mine], _h.Nodes.All.Select(n => n.Id));
    }

    /// <summary>
    /// The upload is judged again under the account lock: one that became active between the listing and the lock (a chunk arrived) is
    /// kept — the transaction is rolled back and nothing is aborted or logged.
    /// </summary>
    [Fact]
    public async Task RemoveStaleUploadsAsync_KeepsAnUploadThatBecameActive_BeforeTheLock()
    {
        long upload = _h.File("slow.iso", uploading: true, created: H.Now.AddHours(-2));
        _h.Accounts.Setup(a => a.FindByEmailForUpdateAsync(H.Email, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await _h.Nodes.RecordUploadActivityAsync(H.Email, upload, H.Now, TestContext.Current.CancellationToken);
                return null;
            });

        Assert.Equal(0, await SweepAsync());

        Assert.NotNull(_h.Nodes.Get(upload));
        _h.AssertRolledBack();
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.DoesNotContain(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Information);
    }

    /// <summary>
    /// An account whose sweep fails is rolled back and logged at Error with its exception, and the next account is still swept; nothing of
    /// the failed account is aborted in file storage.
    /// </summary>
    [Fact]
    public async Task RemoveStaleUploadsAsync_LogsAFailingAccount_AndSweepsTheNext()
    {
        var broken = new InvalidOperationException("lock timeout");
        long theirs = _h.File("theirs.iso", uploading: true, email: H.OtherEmail, created: H.Now.AddHours(-2));
        _h.File("mine.iso", uploading: true, created: H.Now.AddHours(-2));
        Guid theirsId = _h.Node(theirs).StorageId!.Value;
        _h.Accounts.Setup(a => a.FindByEmailForUpdateAsync(H.OtherEmail, It.IsAny<CancellationToken>())).ThrowsAsync(broken);

        Assert.Equal(1, await SweepAsync());

        Assert.Equal([theirs], _h.Nodes.All.Select(n => n.Id));
        _h.UnitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _h.UnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _h.Files.Verify(f => f.AbortFileManagerUploadAsync(theirsId, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        var error = Assert.Single(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Error);
        Assert.Equal($"File Manager: failed to remove the abandoned uploads of {H.OtherEmail}", error.Message);
        Assert.Same(broken, error.Exception);
    }

    /// <summary>A file storage that cannot abort the upload leaves an orphan, logged at Warning; the row stays removed.</summary>
    [Fact]
    public async Task RemoveStaleUploadsAsync_LogsAnUploadFileStorageCouldNotAbort()
    {
        long upload = _h.File("idle.iso", uploading: true, created: H.Now.AddHours(-2));
        Guid uploadId = _h.Node(upload).StorageId!.Value;
        _h.Files.Setup(f => f.AbortFileManagerUploadAsync(uploadId, H.BaseUrl, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        Assert.Equal(1, await SweepAsync());

        Assert.Empty(_h.Nodes.All);
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager: unfinished upload {uploadId} of {H.Email} could not be aborted");
    }
}
