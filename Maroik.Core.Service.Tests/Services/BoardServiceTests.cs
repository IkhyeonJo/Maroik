using System.Data;
using Maroik.Core.Domain.Account;
using Maroik.Core.Domain.Board;
using Maroik.Core.Contract.Dtos;
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
        _unitOfWork.Setup(u => u.BeginAsync(It.IsAny<CancellationToken>(), It.IsAny<IsolationLevel?>())).Returns(Task.CompletedTask);
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
        Board.Reconstitute(id, BoardTypes.FreeForum, "Test Title", "Test Content", writer,
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted, locked, false);

    private static BoardComment MakeComment(long id = 1, string writer = "Alice", long order = 0L) =>
        BoardComment.Reconstitute(id, 1L, order, null, writer, "Comment", DateTime.UtcNow, false);

    // -- GetBoardByIdAsync ----------------------------------------------------

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_DelegatesToRepository()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard());
        var sut = CreateSut();

        BoardResponse? result = await sut.GetBoardByIdAsync(1, ct: TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    /// <summary>
    /// Verifies that <c>GetBoardByIdAsync</c> returns null when the post's type doesn't match
    /// <paramref name="requiredType"/> — e.g. a caller scoped to PrivateNote must not be able to
    /// read a FreeForum post (or vice versa) just by supplying its ID.
    /// </summary>
    [Fact]
    public async Task GetBoardByIdAsync_ReturnsNull_WhenTypeDoesNotMatchRequiredType()
    {
        _boardRepo.Setup(r => r.FindActiveByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MakeBoard()); // Type = BoardTypes.FreeForum
        var sut = CreateSut();

        BoardResponse? result = await sut.GetBoardByIdAsync(1, BoardTypes.PrivateNote, TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    // -- GetBoardPageAsync ------------------------------------------------------

    private static BoardPageQuery MakeQuery() => new(
        BoardTypes.FreeForum, OwnerNickname: null, SearchType: null, SearchText: null,
        IsLoggedIn: false, ViewerRole: null, ViewerNickname: null, Page: 1, PageSize: 10);

    /// <summary>
    /// The count/page read this delegates to is only snapshot-consistent inside a REPEATABLE READ
    /// transaction — asserts the Service (not the repository) opens exactly that isolation level,
    /// and commits it once the repository call returns.
    /// </summary>
    [Fact]
    public async Task GetBoardPageAsync_WrapsRepositoryCallInRepeatableReadTransaction()
    {
        _boardRepo.Setup(r => r.QueryPageAsync(It.IsAny<BoardPageQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(([MakeBoard()], 1));
        var sut = CreateSut();

        var (items, totalCount) = await sut.GetBoardPageAsync(MakeQuery(), TestContext.Current.CancellationToken);

        Assert.Single(items);
        Assert.Equal(1, totalCount);
        _unitOfWork.Verify(u => u.BeginAsync(It.IsAny<CancellationToken>(), IsolationLevel.RepeatableRead), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>A failure inside the transaction rolls back and propagates rather than swallowing the error.</summary>
    [Fact]
    public async Task GetBoardPageAsync_RollsBackAndRethrows_WhenRepositoryThrows()
    {
        _boardRepo.Setup(r => r.QueryPageAsync(It.IsAny<BoardPageQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));
        var sut = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetBoardPageAsync(MakeQuery(), TestContext.Current.CancellationToken));

        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
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
        var board = MakeBoardResponse(BoardTypes.FreeForum, locked: false);

        Assert.True(sut.CanView(board, null));
    }

    [Fact]
    public void CanView_ReturnsFalse_ForLockedFreeForumBoard_WhenViewerIsAnonymous()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(BoardTypes.FreeForum, locked: true);

        Assert.False(sut.CanView(board, null));
    }

    [Fact]
    public void CanView_ReturnsTrue_ForLockedFreeForumBoard_WhenViewerIsWriter()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(BoardTypes.FreeForum, writer: "Alice", locked: true);

        Assert.True(sut.CanView(board, MakeAccount("Alice")));
    }

    [Fact]
    public void CanView_ReturnsTrue_ForLockedFreeForumBoard_WhenViewerIsAdmin()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(BoardTypes.FreeForum, writer: "Alice", locked: true);

        Assert.True(sut.CanView(board, MakeAccount("Bob", Role.Admin)));
    }

    [Fact]
    public void CanView_ReturnsFalse_ForLockedFreeForumBoard_WhenViewerIsUnrelatedUser()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(BoardTypes.FreeForum, writer: "Alice", locked: true);

        Assert.False(sut.CanView(board, MakeAccount("Bob")));
    }

    [Fact]
    public void CanView_ReturnsTrue_ForPrivateNoteBoard_WhenViewerIsWriter()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(BoardTypes.PrivateNote, writer: "Alice", locked: false);

        Assert.True(sut.CanView(board, MakeAccount("Alice")));
    }

    [Fact]
    public void CanView_ReturnsFalse_ForPrivateNoteBoard_WhenViewerIsAnonymous()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(BoardTypes.PrivateNote, writer: "Alice", locked: false);

        Assert.False(sut.CanView(board, null));
    }

    /// <summary>Private notes are visible only to their author — unlike FreeForum, Admin does not bypass this.</summary>
    [Fact]
    public void CanView_ReturnsFalse_ForPrivateNoteBoard_WhenViewerIsAdminButNotWriter()
    {
        var sut = CreateSut();
        var board = MakeBoardResponse(BoardTypes.PrivateNote, writer: "Alice", locked: false);

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
            new BoardRequest { Type = BoardTypes.FreeForum, Title = "Test Title", Writer = "Alice" }, false, null, TestContext.Current.CancellationToken);

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
            new BoardRequest { Type = BoardTypes.FreeForum, Title = "Test Title", Writer = "Alice" }, false, file, TestContext.Current.CancellationToken);

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
            new BoardRequest { Type = BoardTypes.FreeForum, Title = "Test Title", Writer = "Alice" }, false, file, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _attachedFileRepo.Verify(r => r.CreateAsync(It.IsAny<BoardAttachedFile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Regression test: a file whose name is only an extension (e.g. ".zip") produces an empty
    /// Path.GetFileNameWithoutExtension result. The attachment used to be persisted via
    /// BoardAttachedFile.Reconstitute (a validation-bypass method meant only for trusted
    /// repository data), so this empty name slipped through uncaught; it must now go through
    /// BoardAttachedFile.Create and be rejected.
    /// </summary>
    [Fact]
    public async Task WriteBoardAsync_RollsBackAndFails_WhenAttachmentNameIsEmpty()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).ReturnsAsync(1L);
        _fileClient.Setup(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var sut = CreateSut();

        var file = new AttachedFileDto { FileName = ".zip", ContentType = "application/zip", Bytes = [1, 2], Size = 2 };
        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = BoardTypes.FreeForum, Title = "Test Title", Writer = "Alice" }, false, file, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _attachedFileRepo.Verify(r => r.CreateAsync(It.IsAny<BoardAttachedFile>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>WriteBoardAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task WriteBoardAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = BoardTypes.FreeForum, Title = "Test Title", Writer = "Alice" }, false, null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>Noticed=true</c> from a non-admin caller does not pin the post —
    /// enforced by the service itself, not just by the controller that builds the request.</summary>
    [Fact]
    public async Task WriteBoardAsync_DoesNotPin_WhenNoticedTrueButNotAdmin()
    {
        Board? written = null;
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .Callback<Board, CancellationToken>((b, _) => written = b)
            .ReturnsAsync(1L);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = BoardTypes.FreeForum, Title = "Test Title", Writer = "Alice", Noticed = true },
            false, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(written);
        Assert.False(written!.Noticed);
    }

    /// <summary>Verifies that <c>Noticed=true</c> from an admin caller does pin the post.</summary>
    [Fact]
    public async Task WriteBoardAsync_Pins_WhenNoticedTrueAndAdmin()
    {
        Board? written = null;
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .Callback<Board, CancellationToken>((b, _) => written = b)
            .ReturnsAsync(1L);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = BoardTypes.FreeForum, Title = "Test Title", Writer = "Alice", Noticed = true },
            true, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(written);
        Assert.True(written!.Noticed);
    }

    // -- EditBoardAsync -------------------------------------------------------

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 99, Type = BoardTypes.FreeForum }, "Alice", isAdmin: false, null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when board is deleted.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenBoardIsDeleted()
    {
        // FindActiveByIdForUpdateAsync already filters for !Deleted, so deleted board returns null
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 1, Type = BoardTypes.FreeForum }, "Alice", isAdmin: false, null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when requester is not writer.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenRequesterIsNotWriter()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 1, Type = BoardTypes.FreeForum }, "Bob", isAdmin: false, null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns success when writer matches and no new file.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsSuccess_WhenWriterMatchesAndNoNewFile()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "New Title", Content = "New Content" }, "Alice", isAdmin: false, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> rolls back and fails when the new attachment's upload fails, instead of committing an attachment record for a file that was never saved.</summary>
    [Fact]
    public async Task EditBoardAsync_RollsBackAndFails_WhenUploadFails()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        _fileClient.Setup(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var sut = CreateSut();

        var file = new AttachedFileDto { FileName = "doc.zip", ContentType = "application/zip", Bytes = [1, 2], Size = 2 };
        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "New Title", Content = "New Content" }, "Alice", isAdmin: false, file, TestContext.Current.CancellationToken);

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
        Board pinnedBoard = Board.Reconstitute(1, BoardTypes.FreeForum, "Test Title", "Test Content", "Alice",
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted: false, locked: false, noticed: true);
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(pinnedBoard);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        Board? savedBoard = null;
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .Callback<Board, CancellationToken>((b, _) => savedBoard = b)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "New Title", Content = "New Content", Noticed = false },
            "Alice", isAdmin: false, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(savedBoard);
        Assert.True(savedBoard.Noticed);
    }

    /// <summary>
    /// Regression test (L-1): a non-admin author's edit may raise the lock on their own post but
    /// must NOT clear one — clearing a lock is a moderation action reserved for an admin. Without
    /// this, any author edit (BoardRequest.Locked defaults to false) would silently unlock a post
    /// an admin had locked.
    /// </summary>
    [Fact]
    public async Task EditBoardAsync_NonAdminAuthor_CannotClearAnExistingLock()
    {
        Board lockedBoard = Board.Reconstitute(1, BoardTypes.FreeForum, "Test Title", "Test Content", "Alice",
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted: false, locked: true, noticed: false);
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(lockedBoard);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        Board? savedBoard = null;
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .Callback<Board, CancellationToken>((b, _) => savedBoard = b)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "New Title", Content = "New Content", Locked = false },
            "Alice", isAdmin: false, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(savedBoard);
        Assert.True(savedBoard.Locked); // still locked — a non-admin edit cannot lift it
    }

    /// <summary>Companion to <see cref="EditBoardAsync_NonAdminAuthor_CannotClearAnExistingLock"/>: an admin edit CAN clear the lock.</summary>
    [Fact]
    public async Task EditBoardAsync_AdminAuthor_CanClearAnExistingLock()
    {
        Board lockedBoard = Board.Reconstitute(1, BoardTypes.FreeForum, "Test Title", "Test Content", "Alice",
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted: false, locked: true, noticed: false);
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(lockedBoard);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        Board? savedBoard = null;
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .Callback<Board, CancellationToken>((b, _) => savedBoard = b)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "New Title", Content = "New Content", Locked = false },
            "Alice", isAdmin: true, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(savedBoard);
        Assert.False(savedBoard.Locked);
    }

    /// <summary>
    /// Regression test: before this fix, a non-admin author's locked post could never be
    /// unlocked by anyone -- the author isn't admin, and an admin who isn't the author was
    /// rejected by the ownership check before the lock logic ever ran. An admin acting on
    /// someone else's post must be able to clear the lock, without being able to change the
    /// post's title/content.
    /// </summary>
    [Fact]
    public async Task EditBoardAsync_AdminNonOwner_CanClearAnExistingLock_ButNotContent()
    {
        Board lockedBoard = Board.Reconstitute(1, BoardTypes.FreeForum, "Test Title", "Test Content", "Alice",
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted: false, locked: true, noticed: false);
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(lockedBoard);
        _attachedFileRepo.Setup(r => r.FindByBoardIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardAttachedFile?)null);
        Board? savedBoard = null;
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>()))
            .Callback<Board, CancellationToken>((b, _) => savedBoard = b)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "Attempted New Title", Content = "Attempted New Content", Locked = false },
            "Bob", isAdmin: true, null, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.NotNull(savedBoard);
        Assert.False(savedBoard.Locked);
        Assert.Equal("Test Title", savedBoard.Title);
        Assert.Equal("Test Content", savedBoard.Content);
    }

    /// <summary>An admin who is not the writer and does not clear an existing lock is still rejected.</summary>
    [Fact]
    public async Task EditBoardAsync_AdminNonOwner_ReturnsFail_WhenNotClearingALock()
    {
        Board unlockedBoard = MakeBoard(writer: "Alice");
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(unlockedBoard);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = BoardTypes.FreeForum, Title = "New Title", Content = "New Content", Locked = false },
            "Bob", isAdmin: true, null, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // -- DeleteBoardAsync -----------------------------------------------------

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(99, BoardTypes.FreeForum, "Alice", false, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns fail when not owner and not admin.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsFail_WhenNotOwnerAndNotAdmin()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, BoardTypes.FreeForum, "Bob", false, TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns success when owner.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsSuccess_WhenOwner()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, BoardTypes.FreeForum, "Alice", false, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns success when admin.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsSuccess_WhenAdmin()
    {
        _boardRepo.Setup(r => r.FindActiveByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<Board>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, BoardTypes.FreeForum, "Admin", true, TestContext.Current.CancellationToken);

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
        _commentRepo.Setup(r => r.FindByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync((BoardComment?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(99, "Alice", false, ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns fail when not owner and not admin.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsFail_WhenNotOwnerAndNotAdmin()
    {
        _commentRepo.Setup(r => r.FindByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeComment(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(1, "Bob", false, ct: TestContext.Current.CancellationToken);

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
        _unitOfWork.Verify(u => u.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns success when owner.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsSuccess_WhenOwner()
    {
        _commentRepo.Setup(r => r.FindByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeComment(writer: "Alice"));
        _commentRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(1, "Alice", false, ct: TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.BeginAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Regression test: the comment lookup must be the locked (FOR UPDATE) variant, matching this
    /// file's other writes (WriteCommentAsync/EditBoardAsync/DeleteBoardAsync), not the unlocked
    /// FindByIdAsync — otherwise two concurrent deletes of the same comment could both succeed.
    /// </summary>
    [Fact]
    public async Task DeleteCommentAsync_UsesLockedLookup_NotUnlockedFindById()
    {
        _commentRepo.Setup(r => r.FindByIdForUpdateAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(MakeComment(writer: "Alice"));
        _commentRepo.Setup(r => r.UpdateEntityAsync(It.IsAny<BoardComment>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.DeleteCommentAsync(1, "Alice", false, ct: TestContext.Current.CancellationToken);

        _commentRepo.Verify(r => r.FindByIdForUpdateAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        _commentRepo.Verify(r => r.FindByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
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
