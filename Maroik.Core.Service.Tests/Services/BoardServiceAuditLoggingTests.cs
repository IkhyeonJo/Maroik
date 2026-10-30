using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Settings;
using Maroik.Core.Domain.Boards;
using Maroik.Core.Service.Services;
using Maroik.Core.Service.Tests.Fixtures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// The moderation trail of <see cref="BoardService"/>: who deleted whose post or comment, who was refused,
/// and when an admin cleared someone else's lock. Post and comment text is never logged.
/// </summary>
public class BoardServiceAuditLoggingTests : IAsyncDisposable
{
    /// <summary>Fake <c>IBoardRepository</c> injected into the system under test.</summary>
    private readonly FakeBoardRepository _boardRepo = new();
    /// <summary>Fake <c>IBoardAttachedFileRepository</c> injected into the system under test.</summary>
    private readonly FakeBoardAttachedFileRepository _attachedFileRepo = new();
    /// <summary>Fake <c>IBoardCommentRepository</c> injected into the system under test.</summary>
    private readonly FakeBoardCommentRepository _commentRepo = new();
    /// <summary>Fake <c>IUnitOfWork</c> injected into the system under test.</summary>
    private readonly FakeUnitOfWork _unitOfWork = new();
    /// <summary>Fake <c>IAttachmentContentService</c> injected into the system under test.</summary>
    private readonly FakeAttachmentContentService _attachmentContent = new();
    /// <summary>Captures the log entries the system under test writes.</summary>
    private readonly FakeLogger<BoardService> _logger = new();

    /// <summary>Arranges a unit of work that always succeeds, pass-through content decryption, successful updates, and no existing attachment.</summary>
    public BoardServiceAuditLoggingTests()
    {
        _unitOfWork.AnswerBeginAsync((_, _) => Task.CompletedTask);
        _unitOfWork.AnswerCommitAsync(_ => Task.CompletedTask);
        _unitOfWork.AnswerRollbackAsync(_ => Task.CompletedTask);
        _attachmentContent.AnswerSanitizeAndDecryptContent(html => html);
        _boardRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        _commentRepo.AnswerUpdateEntityAsync((_, _) => Task.CompletedTask);
        _attachedFileRepo.AnswerFindByBoardIdAsync((_, _) => Task.FromResult<BoardAttachedFile?>(null));
    }

    /// <summary>The fixed "current time" of these tests.</summary>
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
    /// <summary>The clock the service under test reads, stopped at <see cref="Now"/>.</summary>
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(Now));

    /// <summary>The service under test over the fake dependencies and the capturing logger.</summary>
    private BoardService CreateSut() => new(_boardRepo, _attachedFileRepo, _commentRepo,
        new FakeFileClient(), _unitOfWork, _attachmentContent,
        Options.Create(new ServerSetting { FileStorageBaseUrl = "https://files.example.com" }), _logger, _time);

    /// <summary>A free-forum post whose title and content are secrets that must never be logged.</summary>
    private static Board MakeBoard(string writer = "Alice", bool locked = false) =>
        Board.Reconstitute(1, BoardTypes.FreeForum, "SECRET TITLE", "SECRET CONTENT", writer,
            DateTime.UtcNow, DateTime.UtcNow, 0L, false, locked, false);

    /// <summary>Asserts exactly one entry at <paramref name="level"/> contains <paramref name="containing"/>, and returns it.</summary>
    private FakeLogRecord Only(LogLevel level, string containing)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(),
            r => r.Level == level && r.Message.Contains(containing, StringComparison.Ordinal));
        Assert.DoesNotContain("SECRET", record.Message, StringComparison.Ordinal);
        return record;
    }

    /// <summary>Verifies that an admin deleting someone else's post logs the post, writer, requester and admin flag at Information.</summary>
    [Fact]
    public async Task DeleteBoard_LogsInformationWithWriterRequesterAndAdminFlag_WhenAnAdminDeletesSomeoneElsesPost()
    {
        _boardRepo.AnswerFindActiveByIdForUpdateAsync((_, _) => Task.FromResult<Board?>(MakeBoard()));

        await CreateSut().DeleteBoardAsync(1, BoardTypes.FreeForum, "Admin", true, TestContext.Current.CancellationToken);

        FakeLogRecord record = Only(LogLevel.Information, "Board post 1 of Alice deleted by Admin");
        Assert.Contains("admin: True", record.Message, StringComparison.Ordinal);
    }

    /// <summary>Verifies that a refused post deletion logs a Warning naming the requester and the post.</summary>
    [Fact]
    public async Task DeleteBoard_LogsWarning_WhenTheRequesterMayNotDeleteThePost()
    {
        _boardRepo.AnswerFindActiveByIdForUpdateAsync((_, _) => Task.FromResult<Board?>(MakeBoard()));

        await CreateSut().DeleteBoardAsync(1, BoardTypes.FreeForum, "Bob", false, TestContext.Current.CancellationToken);

        Only(LogLevel.Warning, "Board delete refused: Bob may not delete post 1");
    }

    /// <summary>Verifies that deleting a comment logs the comment, its writer and the requester at Information.</summary>
    [Fact]
    public async Task DeleteComment_LogsInformation_WhenTheCommentIsDeleted()
    {
        _commentRepo.AnswerFindByIdForUpdateAsync((_, _) => Task.FromResult<BoardComment?>(BoardComment.Reconstitute(7, 1L, 0L, null, "Alice", "SECRET COMMENT", DateTime.UtcNow, false)));

        await CreateSut().DeleteCommentAsync(7, "Alice", false, ct: TestContext.Current.CancellationToken);

        Only(LogLevel.Information, "Comment 7 of Alice deleted by Alice");
    }

    /// <summary>Verifies that a refused comment deletion logs a Warning naming the requester and the comment.</summary>
    [Fact]
    public async Task DeleteComment_LogsWarning_WhenTheRequesterMayNotDeleteTheComment()
    {
        _commentRepo.AnswerFindByIdForUpdateAsync((_, _) => Task.FromResult<BoardComment?>(BoardComment.Reconstitute(7, 1L, 0L, null, "Alice", "SECRET COMMENT", DateTime.UtcNow, false)));

        await CreateSut().DeleteCommentAsync(7, "Bob", false, ct: TestContext.Current.CancellationToken);

        Only(LogLevel.Warning, "Comment delete refused: Bob may not delete comment 7");
    }

    /// <summary>Verifies that an edit by someone who is neither the writer nor an admin clearing a lock logs a Warning.</summary>
    [Fact]
    public async Task EditBoard_LogsWarning_WhenTheRequesterIsNeitherTheWriterNorAnAdminClearingALock()
    {
        _boardRepo.AnswerFindActiveByIdForUpdateAsync((_, _) => Task.FromResult<Board?>(MakeBoard()));

        await CreateSut().EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "T", Content = "C", Locked = false },
            "Bob", isAdmin: false, null, TestContext.Current.CancellationToken);

        Only(LogLevel.Warning, "Board edit refused: Bob may not edit post 1");
    }

    /// <summary>Verifies that an admin clearing the lock on someone else's post logs an Information entry.</summary>
    [Fact]
    public async Task EditBoard_LogsInformation_WhenAnAdminClearsSomeoneElsesLock()
    {
        _boardRepo.AnswerFindActiveByIdForUpdateAsync((_, _) => Task.FromResult<Board?>(MakeBoard(locked: true)));

        await CreateSut().EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "T", Content = "C", Locked = false },
            "Admin", isAdmin: true, null, TestContext.Current.CancellationToken);

        Only(LogLevel.Information, "Lock on post 1 of Alice cleared by admin Admin");
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _unitOfWork.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
