using System.Data;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging;
using H = Maroik.Core.Service.Tests.Fixtures.FileManagerServiceHarness;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// The worker's hourly sweep of abandoned uploads, <see cref="FileManagerService.RemoveStaleUploadsAsync"/>: an upload idle for
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
        _h.UnitOfWork.AnswerCommitAsync(_ => { steps.Add("commit"); return Task.CompletedTask; });
        _h.Files.AnswerAbortFileManagerUploadAsync((_, fileStorageBaseUrl, _) => fileStorageBaseUrl == H.BaseUrl, (_, _, _) => { steps.Add("abort"); return Task.FromResult<bool>(true); });
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
        Assert.Single(_h.Files.AbortFileManagerUploadAsyncCalls, call => call.UploadId == idleId && call.FileStorageBaseUrl == H.BaseUrl);
        Assert.Single(_h.Files.AbortFileManagerUploadAsyncCalls, call => call.UploadId == theirsId && call.FileStorageBaseUrl == H.BaseUrl);
        Assert.Equal(["commit", "abort", "commit", "abort"], steps);
        Assert.Single(_h.Accounts.FindByEmailForUpdateAsyncCalls, call => call.Email == H.Email);
        Assert.Single(_h.Accounts.FindByEmailForUpdateAsyncCalls, call => call.Email == H.OtherEmail);
        Assert.Equal(2, _h.UnitOfWork.BeginAsyncCalls.Count);
        Assert.Empty(_h.UnitOfWork.RollbackAsyncCalls);
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
        Assert.Empty(_h.UnitOfWork.BeginAsyncCalls);
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
        _h.Accounts.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == H.Email, async (_, _) => {
                await _h.Nodes.RecordUploadActivityAsync(H.Email, upload, H.Now, TestContext.Current.CancellationToken);
                return null;
            });

        Assert.Equal(0, await SweepAsync());

        Assert.NotNull(_h.Nodes.Get(upload));
        _h.AssertRolledBack();
        Assert.Empty(_h.Files.AbortFileManagerUploadAsyncCalls);
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
        _h.Accounts.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == H.OtherEmail, (_, _) => Task.FromException<Account?>(broken));

        Assert.Equal(1, await SweepAsync());

        Assert.Equal([theirs], _h.Nodes.All.Select(n => n.Id));
        Assert.Single(_h.UnitOfWork.RollbackAsyncCalls);
        Assert.Single(_h.UnitOfWork.CommitAsyncCalls);
        Assert.DoesNotContain(_h.Files.AbortFileManagerUploadAsyncCalls, call => call.UploadId == theirsId);
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
        _h.Files.AnswerAbortFileManagerUploadAsync((uploadIdArg, fileStorageBaseUrl, _) => uploadIdArg == uploadId && fileStorageBaseUrl == H.BaseUrl, (_, _, _) => Task.FromResult<bool>(false));

        Assert.Equal(1, await SweepAsync());

        Assert.Empty(_h.Nodes.All);
        Assert.Contains(_h.Logger.Collector.GetSnapshot(), r => r.Level == LogLevel.Warning && r.Message == $"File Manager: unfinished upload {uploadId} of {H.Email} could not be aborted");
    }
}
