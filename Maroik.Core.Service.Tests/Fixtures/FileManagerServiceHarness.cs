using Maroik.Core.Contract.Enums;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Accounts;
using Maroik.Core.Domain.FileManagers;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Fixtures;

/// <summary>
/// Builds a <see cref="FileManagerService"/> for its tests: an in-memory node repository, a fake account repository answering one
/// account with <see cref="QuotaBytes"/>, a fake unit of work, a fake file client that accepts everything unless told otherwise, a
/// captured log and a stopped clock. Seeding helpers add folders and files of <see cref="Email"/>.
/// </summary>
internal sealed class FileManagerServiceHarness
{
    /// <summary>The account the tests act as.</summary>
    public const string Email = "user@example.com";

    /// <summary>Another account, whose items must stay invisible.</summary>
    public const string OtherEmail = "other@example.com";

    /// <summary>The base URL the service passes to the file client.</summary>
    public const string BaseUrl = "http://files.local";

    /// <summary>One GB in bytes.</summary>
    public const long Gb = 1024L * 1024 * 1024;

    /// <summary>The fixed "current time".</summary>
    public static readonly DateTime Now = new(2026, 5, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>The nodes.</summary>
    public InMemoryFileManagerNodeRepository Nodes { get; } = new();

    /// <summary>The account repository.</summary>
    public FakeAccountRepository Accounts { get; } = new();

    /// <summary>The unit of work.</summary>
    public FakeUnitOfWork UnitOfWork { get; } = new();

    /// <summary>The file client.</summary>
    public FakeFileClient Files { get; } = new();

    /// <summary>The captured log.</summary>
    public FakeLogger<FileManagerService> Logger { get; } = new();

    /// <summary>The stopped clock.</summary>
    public FakeTimeProvider Time { get; } = new(new DateTimeOffset(Now));

    /// <summary>The account's File Manager quota.</summary>
    public long QuotaBytes { get; set; } = 10 * Gb;

    /// <summary>Whether the account's virus scan is turned off (as the unlocked read sees it).</summary>
    public bool ScanDisabled { get; set; }

    /// <summary>The same, as the locked read inside a transaction sees it (an administrator may change it in between); null = <see cref="ScanDisabled"/>.</summary>
    public bool? ScanDisabledUnderLock { get; set; }

    /// <summary>Wires the fakes.</summary>
    public FileManagerServiceHarness()
    {
        Accounts.AnswerFindByEmailForUpdateAsync((emailArg, _) => emailArg == Email, (_, _) => Task.FromResult<Account?>(Account(ScanDisabledUnderLock ?? ScanDisabled)));
        Accounts.AnswerFindByEmailAsync((emailArg, _) => emailArg == Email, (_, _) => Task.FromResult<Account?>(Account(ScanDisabled)));
        UnitOfWork.AnswerBeginAsync((_, _) => Task.CompletedTask);
        UnitOfWork.AnswerCommitAsync(_ => Task.CompletedTask);
        UnitOfWork.AnswerRollbackAsync(_ => Task.CompletedTask);
        Files.AnswerAppendFileManagerChunkAsync((_, _, _, fileStorageBaseUrl, _) => fileStorageBaseUrl == BaseUrl, (_, offset, chunk, _, _) => Task.FromResult<long?>(offset + chunk.Length));
        Files.AnswerCompleteFileManagerUploadAsync((_, _, _, fileStorageBaseUrl, _) => fileStorageBaseUrl == BaseUrl, (_, _, _, _, _) => Task.FromResult<FileUploadResult>(FileUploadResult.Stored));
        Files.AnswerAbortFileManagerUploadAsync((_, fileStorageBaseUrl, _) => fileStorageBaseUrl == BaseUrl, (_, _, _) => Task.FromResult<bool>(true));
        Files.AnswerDeleteFileManagerFileAsync((_, fileStorageBaseUrl, _) => fileStorageBaseUrl == BaseUrl, (_, _, _) => Task.FromResult<bool>(true));
    }

    /// <summary>The account, as the repository loads it.</summary>
    private Account Account(bool scanDisabled) => Domain.Accounts.Account.Reconstitute(Email, "hash", "User", null, "User", "UTC", null, false, 0, true, true,
        null, null, Now, Now, null, false, "stamp", false, QuotaBytes, scanDisabled);

    /// <summary>The service under test.</summary>
    public FileManagerService Sut => new(Nodes, Accounts, UnitOfWork, Files,
        Options.Create(new ServerSetting { FileStorageBaseUrl = BaseUrl }), Logger, Time);

    /// <summary>Adds a folder and returns its id.</summary>
    public long Folder(string name, long? parentId = null, string email = Email)
    {
        _ = Nodes.CreateAsync(FileManagerNode.CreateFolder(email, parentId, name, Now).Value);
        return Nodes.All.Max(n => n.Id);
    }

    /// <summary>Adds a file (finished unless <paramref name="uploading"/>) and returns its id.</summary>
    public long File(string name, long? parentId = null, long size = 10, bool uploading = false, string email = Email, DateTime? created = null)
    {
        FileManagerNode file = FileManagerNode.StartUpload(email, parentId, name, size, Guid.NewGuid(), created ?? Now, scanDisabled: false).Value;
        if (!uploading) file.CompleteUpload(created ?? Now);
        _ = Nodes.CreateAsync(file);
        return Nodes.All.Max(n => n.Id);
    }

    /// <summary>The stored node with <paramref name="id"/> (must exist).</summary>
    public FileManagerNode Node(long id) => Nodes.Get(id) ?? throw new InvalidOperationException($"No node {id}");

    /// <summary>Asserts the unit of work ran Begin and then Commit (no Rollback).</summary>
    public void AssertCommitted()
    {
        Assert.Single(UnitOfWork.BeginAsyncCalls);
        Assert.Single(UnitOfWork.CommitAsyncCalls);
        Assert.Empty(UnitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Asserts the unit of work ran Begin and then Rollback (no Commit).</summary>
    public void AssertRolledBack()
    {
        Assert.Single(UnitOfWork.BeginAsyncCalls);
        Assert.Empty(UnitOfWork.CommitAsyncCalls);
        Assert.Single(UnitOfWork.RollbackAsyncCalls);
    }

    /// <summary>Asserts the account row was locked before the write.</summary>
    public void AssertAccountLocked() => Assert.Single(Accounts.FindByEmailForUpdateAsyncCalls, call => call.Email == Email);
}
