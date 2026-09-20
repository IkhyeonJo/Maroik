using System.Data;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Domain.Board;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// The moderation trail of <see cref="BoardService"/>: who deleted whose post or comment, who was refused,
/// and when an admin cleared someone else's lock. Post and comment text is never logged.
/// </summary>
public class BoardServiceAuditLoggingTests
{
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IBoardAttachedFileRepository> _attachedFileRepo = new();
    private readonly Mock<IBoardCommentRepository> _commentRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAttachmentContentService> _attachmentContent = new();
    private readonly FakeLogger<BoardService> _logger = new();

    public BoardServiceAuditLoggingTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync(It.IsAny<CancellationToken>(), It.IsAny<IsolationLevel?>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _attachmentContent.Setup(s => s.SanitizeAndDecryptContent(It.IsAny<string>())).Returns<string>(html => html);
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _commentRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
    }

    private BoardService CreateSut() => new(_boardRepo.Object, _attachedFileRepo.Object, _commentRepo.Object,
        Mock.Of<IFileClient>(), _unitOfWork.Object, _attachmentContent.Object,
        Options.Create(new ServerSetting { FileStorageBaseUrl = "http://files.example.com" }), _logger);

    private static Board MakeBoard(string writer = "Alice", bool locked = false) =>
        Board.Reconstitute(1, BoardTypes.FreeForum, "SECRET TITLE", "SECRET CONTENT", writer,
            DateTime.UtcNow, DateTime.UtcNow, 0L, false, locked, false);

    private FakeLogRecord Only(LogLevel level, string containing)
    {
        FakeLogRecord record = Assert.Single(_logger.Collector.GetSnapshot(),
            r => r.Level == level && r.Message.Contains(containing, StringComparison.Ordinal));
        Assert.DoesNotContain("SECRET", record.Message);
        return record;
    }

    [Fact]
    public async Task DeleteBoard_LogsInformationWithWriterRequesterAndAdminFlag_WhenAnAdminDeletesSomeoneElsesPost()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard("Alice"));

        await CreateSut().DeleteBoardAsync(1, BoardTypes.FreeForum, "Admin", true, TestContext.Current.CancellationToken);

        FakeLogRecord record = Only(LogLevel.Information, "Board post 1 of Alice deleted by Admin");
        Assert.Contains("admin: True", record.Message);
    }

    [Fact]
    public async Task DeleteBoard_LogsWarning_WhenTheRequesterMayNotDeleteThePost()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard("Alice"));

        await CreateSut().DeleteBoardAsync(1, BoardTypes.FreeForum, "Bob", false, TestContext.Current.CancellationToken);

        Only(LogLevel.Warning, "Board delete refused: Bob may not delete post 1");
    }

    [Fact]
    public async Task DeleteComment_LogsInformation_WhenTheCommentIsDeleted()
    {
        _commentRepo.Setup(r => r.FindByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BoardComment.Reconstitute(7, 1L, 0L, null, "Alice", "SECRET COMMENT", DateTime.UtcNow, false));

        await CreateSut().DeleteCommentAsync(7, "Alice", false, ct: TestContext.Current.CancellationToken);

        Only(LogLevel.Information, "Comment 7 of Alice deleted by Alice");
    }

    [Fact]
    public async Task DeleteComment_LogsWarning_WhenTheRequesterMayNotDeleteTheComment()
    {
        _commentRepo.Setup(r => r.FindByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BoardComment.Reconstitute(7, 1L, 0L, null, "Alice", "SECRET COMMENT", DateTime.UtcNow, false));

        await CreateSut().DeleteCommentAsync(7, "Bob", false, ct: TestContext.Current.CancellationToken);

        Only(LogLevel.Warning, "Comment delete refused: Bob may not delete comment 7");
    }

    [Fact]
    public async Task EditBoard_LogsWarning_WhenTheRequesterIsNeitherTheWriterNorAnAdminClearingALock()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard("Alice"));

        await CreateSut().EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "T", Content = "C", Locked = false },
            "Bob", isAdmin: false, null, TestContext.Current.CancellationToken);

        Only(LogLevel.Warning, "Board edit refused: Bob may not edit post 1");
    }

    [Fact]
    public async Task EditBoard_LogsInformation_WhenAnAdminClearsSomeoneElsesLock()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard("Alice", locked: true));

        await CreateSut().EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "T", Content = "C", Locked = false },
            "Admin", isAdmin: true, null, TestContext.Current.CancellationToken);

        Only(LogLevel.Information, "Lock on post 1 of Alice cleared by admin Admin");
    }
}
