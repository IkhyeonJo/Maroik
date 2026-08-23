using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Board;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Misc.Enums;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Misc.Settings;
using Maroik.Core.Service.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="BoardService"/>.
/// All external dependencies (repositories, ClamAV client, file client, logger)
/// are replaced with Moq mocks. Covers board post CRUD, comment management,
/// attachment upload/delete, and view-count incrementing.
/// </summary>
public class BoardServiceTests
{
    private readonly Mock<IBoardRepository> _boardRepo = new();
    private readonly Mock<IBoardAttachedFileRepository> _attachedFileRepo = new();
    private readonly Mock<IBoardCommentRepository> _commentRepo = new();
    private readonly Mock<IFileClient> _fileClient = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAttachmentContentService> _attachmentContent = new();
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { FileStorageBaseUrl = "http://files.example.com" });

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public BoardServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _attachmentContent.Setup(s => s.SanitizeAndDecryptContent(It.IsAny<string>())).Returns<string>(html => html);
        _attachmentContent.Setup(s => s.SanitizeContent(It.IsAny<string>())).Returns<string>(html => html);
    }

    private BoardService CreateSut() => new(
        _boardRepo.Object,
        _attachedFileRepo.Object,
        _commentRepo.Object,
        _fileClient.Object,
        _unitOfWork.Object,
        _attachmentContent.Object,
        _settings,
        NullLogger<BoardService>.Instance);

    // -- Helpers --------------------------------------------------------------

    private static Board MakeBoard(long id = 1, string writer = "Alice", bool deleted = false, bool locked = false) =>
        Board.Reconstitute(id, nameof(BoardType.FreeForum), "Test Title", "Test Content", writer,
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted, locked, false);

    private static BoardComment MakeComment(long id = 1, string writer = "Alice", long order = 0L) =>
        BoardComment.Reconstitute(id, 1L, order, null, writer, "Comment", DateTime.UtcNow, false);

    // -- GetBoardsAsync -------------------------------------------------------

    /// <summary>Verifies that <c>GetBoardsAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetBoardsAsync_DelegatesToRepository()
    {
        var list = new List<Board> { MakeBoard() };
        _boardRepo.Setup(r => r.GetByTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(list);
        var sut = CreateSut();

        IEnumerable<BoardResponse> result = await sut.GetBoardsAsync(nameof(BoardType.FreeForum), TestContext.Current.CancellationToken);

        Assert.Single(result);
    }

    // -- GetBoardByIdAsync ----------------------------------------------------

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_DelegatesToRepository()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard());
        var sut = CreateSut();

        BoardResponse? result = await sut.GetBoardByIdAsync(1, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    // -- CanView ----------------------------------------------------------------

    private static BoardResponse MakeBoardResponse(string type, string writer = "Alice", bool locked = false) =>
        new() { Type = type, Writer = writer, Locked = locked };

    private static AccountResponse MakeAccount(string nickname, string role = Role.User) =>
        new() { Nickname = nickname, Role = role };

    [Fact]
    public void CanView_ReturnsTrue_ForUnlockedFreeForumBoard_WhenViewerIsAnonymous()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.FreeForum), locked: false);

        Assert.True(sut.CanView(board, null));
    }

    [Fact]
    public void CanView_ReturnsFalse_ForLockedFreeForumBoard_WhenViewerIsAnonymous()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.FreeForum), locked: true);

        Assert.False(sut.CanView(board, null));
    }

    [Fact]
    public void CanView_ReturnsTrue_ForLockedFreeForumBoard_WhenViewerIsWriter()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.FreeForum), writer: "Alice", locked: true);

        Assert.True(sut.CanView(board, MakeAccount("Alice")));
    }

    [Fact]
    public void CanView_ReturnsTrue_ForLockedFreeForumBoard_WhenViewerIsAdmin()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.FreeForum), writer: "Alice", locked: true);

        Assert.True(sut.CanView(board, MakeAccount("Bob", Role.Admin)));
    }

    [Fact]
    public void CanView_ReturnsFalse_ForLockedFreeForumBoard_WhenViewerIsUnrelatedUser()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.FreeForum), writer: "Alice", locked: true);

        Assert.False(sut.CanView(board, MakeAccount("Bob")));
    }

    [Fact]
    public void CanView_ReturnsTrue_ForPrivateNoteBoard_WhenViewerIsWriter()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.PrivateNote), writer: "Alice", locked: false);

        Assert.True(sut.CanView(board, MakeAccount("Alice")));
    }

    [Fact]
    public void CanView_ReturnsFalse_ForPrivateNoteBoard_WhenViewerIsAnonymous()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.PrivateNote), writer: "Alice", locked: false);

        Assert.False(sut.CanView(board, null));
    }

    /// <summary>Private notes are visible only to their author — unlike FreeForum, Admin does not bypass this.</summary>
    [Fact]
    public void CanView_ReturnsFalse_ForPrivateNoteBoard_WhenViewerIsAdminButNotWriter()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(nameof(BoardType.PrivateNote), writer: "Alice", locked: false);

        Assert.False(sut.CanView(board, MakeAccount("Bob", Role.Admin)));
    }

    // -- WriteBoardAsync ------------------------------------------------------

    /// <summary>Verifies that <c>WriteBoardAsync</c> returns success with no attachment.</summary>
    [Fact]
    public async Task WriteBoardAsync_ReturnsSuccess_WithNoAttachment()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = nameof(BoardType.FreeForum), Title = "Test Title", Writer = "Alice" }, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>WriteBoardAsync</c> returns success with attachment.</summary>
    [Fact]
    public async Task WriteBoardAsync_ReturnsSuccess_WithAttachment()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        _fileClient.Setup(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _attachedFileRepo.Setup(r => r.CreateAsync(It.IsAny<BoardAttachedFile>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var file = new AttachedFileDto { FileName = "doc.zip", ContentType = "application/zip", Bytes = [1, 2], Size = 2 };
        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = nameof(BoardType.FreeForum), Title = "Test Title", Writer = "Alice" }, file, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _attachedFileRepo.Verify(r => r.CreateAsync(It.IsAny<BoardAttachedFile>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>WriteBoardAsync</c> rolls back and fails when the file upload itself fails, instead of committing an attachment record for a file that was never saved.</summary>
    [Fact]
    public async Task WriteBoardAsync_RollsBackAndFails_WhenUploadFails()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        _fileClient.Setup(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut();

        var file = new AttachedFileDto { FileName = "doc.zip", ContentType = "application/zip", Bytes = [1, 2], Size = 2 };
        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = nameof(BoardType.FreeForum), Title = "Test Title", Writer = "Alice" }, file, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _attachedFileRepo.Verify(r => r.CreateAsync(It.IsAny<BoardAttachedFile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>Verifies that <c>WriteBoardAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task WriteBoardAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = nameof(BoardType.FreeForum), Title = "Test Title", Writer = "Alice" }, null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- EditBoardAsync -------------------------------------------------------

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 99, Type = nameof(BoardType.FreeForum) }, "Alice", null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when board is deleted.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenBoardIsDeleted()
    {
        // FindActiveByIdAsync already filters for !Deleted, so deleted board returns null
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 1, Type = nameof(BoardType.FreeForum) }, "Alice", null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when requester is not writer.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenRequesterIsNotWriter()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 1, Type = nameof(BoardType.FreeForum) }, "Bob", null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns success when writer matches and no new file.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsSuccess_WhenWriterMatchesAndNoNewFile()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = nameof(BoardType.FreeForum), Title = "New Title", Content = "New Content" }, "Alice", null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> rolls back and fails when the new attachment's upload fails, instead of committing an attachment record for a file that was never saved.</summary>
    [Fact]
    public async Task EditBoardAsync_RollsBackAndFails_WhenUploadFails()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        _fileClient.Setup(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut();

        var file = new AttachedFileDto { FileName = "doc.zip", ContentType = "application/zip", Bytes = [1, 2], Size = 2 };
        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = nameof(BoardType.FreeForum), Title = "New Title", Content = "New Content" }, "Alice", file, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _attachedFileRepo.Verify(r => r.CreateAsync(It.IsAny<BoardAttachedFile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Regression test: no edit form exposes a "Noticed" control, so <c>BoardRequest.Noticed</c>
    /// is always false on an edit request. Editing must not un-pin an already-pinned post.
    /// </summary>
    [Fact]
    public async Task EditBoardAsync_PreservesNoticed_WhenRequestDoesNotSpecifyIt()
    {
        Board pinnedBoard = Board.Reconstitute(1, nameof(BoardType.FreeForum), "Test Title", "Test Content", "Alice",
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted: false, locked: false, noticed: true);
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(pinnedBoard);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        Board? savedBoard = null;
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .Callback<Board, CancellationToken>((b, _) => savedBoard = b)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = nameof(BoardType.FreeForum), Title = "New Title", Content = "New Content", Noticed = false },
            "Alice", null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(savedBoard);
        Assert.True(savedBoard.Noticed);
    }

    // -- DeleteBoardAsync -----------------------------------------------------

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(99, nameof(BoardType.FreeForum), "Alice", false, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns fail when not owner and not admin.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsFail_WhenNotOwnerAndNotAdmin()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, nameof(BoardType.FreeForum), "Bob", false, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns success when owner.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsSuccess_WhenOwner()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, nameof(BoardType.FreeForum), "Alice", false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns success when admin.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsSuccess_WhenAdmin()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, nameof(BoardType.FreeForum), "Admin", true, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    // -- WriteCommentAsync ----------------------------------------------------

    /// <summary>Verifies that <c>WriteCommentAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 99 }, ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> returns fail when required owner does not match.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsFail_WhenRequiredOwnerDoesNotMatch()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1 }, "Bob", ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> returns success when owner matches and no owner restriction.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsSuccess_WhenOwnerMatchesAndNoOwnerRestriction()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _commentRepo.Setup(r => r.GetByBoardIdOrderedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _commentRepo.Setup(r => r.CreateAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hello" }, ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> fails on a locked board when the commenter is neither owner nor admin.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsFail_WhenBoardLockedAndCommenterIsNotOwnerOrAdmin()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice", locked: true));
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hi" }, ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> still succeeds on a locked board when the commenter is the post's owner.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsSuccess_WhenBoardLockedAndCommenterIsOwner()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice", locked: true));
        _commentRepo.Setup(r => r.GetByBoardIdOrderedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _commentRepo.Setup(r => r.CreateAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Alice", Content = "Hi" }, ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> still succeeds on a locked board when the commenter is an admin.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsSuccess_WhenBoardLockedAndCommenterIsAdmin()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice", locked: true));
        _commentRepo.Setup(r => r.GetByBoardIdOrderedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _commentRepo.Setup(r => r.CreateAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hi" }, isAdmin: true, ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> sets order based on existing comment count.</summary>
    [Fact]
    public async Task WriteCommentAsync_SetsOrderBasedOnHighestExistingCommentOrder()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard());
        _commentRepo.Setup(r => r.GetByBoardIdOrderedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BoardComment> { MakeComment(1, order: 0L), MakeComment(2, order: 1L) });

        BoardComment? captured = null;
        _commentRepo.Setup(r => r.CreateAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>()))
            .Callback<BoardComment, CancellationToken>((c, _) => captured = c)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hi" }, ct: TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal(2L, captured.Order);
    }

    /// <summary>
    /// Regression test: after a comment is soft-deleted, <c>GetByBoardIdOrderedAsync</c> excludes it,
    /// so the remaining comments' count is no longer <c>max(Order) + 1</c>. The new comment's Order
    /// must still be derived from the highest surviving Order, not the count, or it collides with an
    /// existing comment's Order.
    /// </summary>
    [Fact]
    public async Task WriteCommentAsync_DoesNotCollideWithExistingOrder_AfterAPriorCommentWasDeleted()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard());
        // Order 1 is "missing" here, simulating a soft-deleted comment excluded by the repository query.
        _commentRepo.Setup(r => r.GetByBoardIdOrderedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BoardComment> { MakeComment(1, order: 0L), MakeComment(3, order: 2L) });

        BoardComment? captured = null;
        _commentRepo.Setup(r => r.CreateAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>()))
            .Callback<BoardComment, CancellationToken>((c, _) => captured = c)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hi" }, ct: TestContext.Current.CancellationToken);

        Assert.NotNull(captured);
        Assert.Equal(3L, captured.Order);
    }

    /// <summary>
    /// Regression test: comment writes must lock the parent post row (FOR UPDATE) inside a
    /// transaction, so concurrent commenters on the same post serialize instead of racing to
    /// read the same <c>Count()</c> and insert duplicate <c>Order</c> values.
    /// </summary>
    [Fact]
    public async Task WriteCommentAsync_LocksBoardRowInTransaction()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard());
        _commentRepo.Setup(r => r.GetByBoardIdOrderedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        _commentRepo.Setup(r => r.CreateAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hi" }, ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _boardRepo.Verify(r => r.FindActiveByIdForUpdateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- DeleteCommentAsync ---------------------------------------------------

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns fail when comment not found.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsFail_WhenCommentNotFound()
    {
        _commentRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardComment?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(99, "Alice", false, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns fail when not owner and not admin.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsFail_WhenNotOwnerAndNotAdmin()
    {
        _commentRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeComment(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(1, "Bob", false, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns success when owner.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsSuccess_WhenOwner()
    {
        _commentRepo.Setup(r => r.FindByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeComment(writer: "Alice"));
        _commentRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(1, "Alice", false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    // -- IncrementBoardViewAsync ----------------------------------------------

    /// <summary>Verifies that <c>IncrementBoardViewAsync</c> returns success when no exception.</summary>
    [Fact]
    public async Task IncrementBoardViewAsync_ReturnsSuccess_WhenNoException()
    {
        _boardRepo.Setup(r => r.IncrementViewAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.IncrementBoardViewAsync(1, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _boardRepo.Verify(r => r.IncrementViewAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>IncrementBoardViewAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task IncrementBoardViewAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _boardRepo.Setup(r => r.IncrementViewAsync(1, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("error"));
        var sut = CreateSut();

        ServiceResult result = await sut.IncrementBoardViewAsync(1, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }
}
