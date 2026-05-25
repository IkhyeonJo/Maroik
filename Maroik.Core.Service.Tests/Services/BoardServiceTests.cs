using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.Contract.Utilities;
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
    private readonly Mock<IRsaService> _rsa = new();
    private readonly IOptions<ServerSetting> _settings =
        Options.Create(new ServerSetting { FileStorageBaseUrl = "http://files.example.com" });

    /// <summary>Initializes the test fixture, setting up all required test doubles and the system under test.</summary>
    public BoardServiceTests()
    {
        _unitOfWork.Setup(u => u.BeginAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.RollbackAsync()).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.DisposeAsync()).Returns(ValueTask.CompletedTask);
    }

    private BoardService CreateSut() => new(
        _boardRepo.Object,
        _attachedFileRepo.Object,
        _commentRepo.Object,
        _fileClient.Object,
        _unitOfWork.Object,
        _rsa.Object,
        _settings,
        NullLogger<BoardService>.Instance);

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Board MakeBoard(long id = 1, string writer = "Alice", bool deleted = false) =>
        Board.Reconstitute(id, "FreeForum", "Test Title", "Test Content", writer,
            DateTime.UtcNow, DateTime.UtcNow, 0L, deleted, false, false);

    private static BoardComment MakeComment(long id = 1, string writer = "Alice") =>
        BoardComment.Reconstitute(id, 1L, 0L, null, writer, "Comment", DateTime.UtcNow, false);

    // ── GetBoardsAsync ───────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetBoardsAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetBoardsAsync_DelegatesToRepository()
    {
        var list = new List<Board> { MakeBoard() };
        _boardRepo.Setup(r => r.GetOneTypeBoardsAsync("FreeForum")).ReturnsAsync(list);
        var sut = CreateSut();

        IEnumerable<BoardResponse> result = await sut.GetBoardsAsync("FreeForum");

        Assert.Single(result);
    }

    // ── GetBoardByIdAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>GetBoardByIdAsync</c> delegates to repository.</summary>
    [Fact]
    public async Task GetBoardByIdAsync_DelegatesToRepository()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard());
        var sut = CreateSut();

        BoardResponse? result = await sut.GetBoardByIdAsync(1);

        Assert.NotNull(result);
        Assert.Equal(1, result.Id);
    }

    // ── WriteBoardAsync ──────────────────────────────────────────────────────

    /// <summary>Verifies that <c>WriteBoardAsync</c> returns success with no attachment.</summary>
    [Fact]
    public async Task WriteBoardAsync_ReturnsSuccess_WithNoAttachment()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>())).ReturnsAsync(1L);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = "FreeForum", Title = "Test Title", Writer = "Alice" }, null);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>WriteBoardAsync</c> returns success with attachment.</summary>
    [Fact]
    public async Task WriteBoardAsync_ReturnsSuccess_WithAttachment()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>())).ReturnsAsync(1L);
        _fileClient.Setup(f => f.UploadAsync(It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _attachedFileRepo.Setup(r => r.SaveBoardAttachedFileAsync(It.IsAny<BoardAttachedFile>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        var file = new AttachedFileInfo { FileName = "doc.zip", ContentType = "application/zip", Bytes = [1, 2], Size = 2 };
        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = "FreeForum", Title = "Test Title", Writer = "Alice" }, file);

        Assert.True(result.Success);
        _attachedFileRepo.Verify(r => r.SaveBoardAttachedFileAsync(It.IsAny<BoardAttachedFile>()), Times.Once);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>WriteBoardAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task WriteBoardAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _boardRepo.Setup(r => r.WriteBoardAsync(It.IsAny<Board>())).ThrowsAsync(new Exception("DB error"));
        var sut = CreateSut();

        ServiceResult result = await sut.WriteBoardAsync(
            new BoardRequest { Type = "FreeForum", Title = "Test Title", Writer = "Alice" }, null);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    // ── EditBoardAsync ───────────────────────────────────────────────────────

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(99)).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 99, Type = "FreeForum" }, "Alice", null);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when board is deleted.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenBoardIsDeleted()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(deleted: true));
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 1, Type = "FreeForum" }, "Alice", null);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns fail when requester is not writer.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsFail_WhenRequesterIsNotWriter()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(new BoardRequest { Id = 1, Type = "FreeForum" }, "Bob", null);

        Assert.False(result.Success);
        _unitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
    }

    /// <summary>Verifies that <c>EditBoardAsync</c> returns success when writer matches and no new file.</summary>
    [Fact]
    public async Task EditBoardAsync_ReturnsSuccess_WhenWriterMatchesAndNoNewFile()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.UpdateBoardAsync(It.IsAny<Board>())).Returns(Task.CompletedTask);
        _attachedFileRepo.Setup(r => r.GetByBoardIdAsync(1)).ReturnsAsync((BoardAttachedFile?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.EditBoardAsync(
            new BoardRequest { Id = 1, Type = "FreeForum", Title = "New Title", Content = "New Content" }, "Alice", null);

        Assert.True(result.Success);
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
    }

    // ── DeleteBoardAsync ─────────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(99)).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(99, "FreeForum", "Alice", false);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns fail when not owner and not admin.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsFail_WhenNotOwnerAndNotAdmin()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, "FreeForum", "Bob", false);

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns success when owner.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsSuccess_WhenOwner()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.DeleteBoardAsync(It.IsAny<Board>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, "FreeForum", "Alice", false);

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>DeleteBoardAsync</c> returns success when admin.</summary>
    [Fact]
    public async Task DeleteBoardAsync_ReturnsSuccess_WhenAdmin()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(writer: "Alice"));
        _boardRepo.Setup(r => r.DeleteBoardAsync(It.IsAny<Board>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteBoardAsync(1, "FreeForum", "Admin", true);

        Assert.True(result.Success);
    }

    // ── WriteCommentAsync ────────────────────────────────────────────────────

    /// <summary>Verifies that <c>WriteCommentAsync</c> returns fail when board not found.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsFail_WhenBoardNotFound()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(99)).ReturnsAsync((Board?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 99 });

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> returns fail when required owner does not match.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsFail_WhenRequiredOwnerDoesNotMatch()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1 }, "Bob");

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> returns success when owner matches and no owner restriction.</summary>
    [Fact]
    public async Task WriteCommentAsync_ReturnsSuccess_WhenOwnerMatchesAndNoOwnerRestriction()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard(writer: "Alice"));
        _commentRepo.Setup(r => r.GetOneBoardCommentsAsync(1)).ReturnsAsync(new List<BoardComment>());
        _commentRepo.Setup(r => r.WriteBoardCommentAsync(It.IsAny<BoardComment>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hello" });

        Assert.True(result.Success);
    }

    /// <summary>Verifies that <c>WriteCommentAsync</c> sets order based on existing comment count.</summary>
    [Fact]
    public async Task WriteCommentAsync_SetsOrderBasedOnExistingCommentCount()
    {
        _boardRepo.Setup(r => r.GetBoardByIdAsync(1)).ReturnsAsync(MakeBoard());
        _commentRepo.Setup(r => r.GetOneBoardCommentsAsync(1))
            .ReturnsAsync(new List<BoardComment> { MakeComment(1), MakeComment(2) });

        BoardComment? captured = null;
        _commentRepo.Setup(r => r.WriteBoardCommentAsync(It.IsAny<BoardComment>()))
            .Callback<BoardComment>(c => captured = c)
            .Returns(Task.CompletedTask);
        var sut = CreateSut();

        await sut.WriteCommentAsync(new BoardCommentRequest { BoardId = 1, Writer = "Bob", Content = "Hi" });

        Assert.NotNull(captured);
        Assert.Equal(2L, captured.Order);
    }

    // ── DeleteCommentAsync ───────────────────────────────────────────────────

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns fail when comment not found.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsFail_WhenCommentNotFound()
    {
        _commentRepo.Setup(r => r.GetBoardCommentAsync(99)).ReturnsAsync((BoardComment?)null);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(99, "Alice", false);

        Assert.False(result.Success);
    }

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns fail when not owner and not admin.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsFail_WhenNotOwnerAndNotAdmin()
    {
        _commentRepo.Setup(r => r.GetBoardCommentAsync(1)).ReturnsAsync(MakeComment(writer: "Alice"));
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(1, "Bob", false);

        Assert.False(result.Success);
        Assert.Contains("permission", result.ErrorKey, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that <c>DeleteCommentAsync</c> returns success when owner.</summary>
    [Fact]
    public async Task DeleteCommentAsync_ReturnsSuccess_WhenOwner()
    {
        _commentRepo.Setup(r => r.GetBoardCommentAsync(1)).ReturnsAsync(MakeComment(writer: "Alice"));
        _commentRepo.Setup(r => r.DeleteBoardCommentAsync(It.IsAny<BoardComment>())).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.DeleteCommentAsync(1, "Alice", false);

        Assert.True(result.Success);
    }

    // ── IncrementBoardViewAsync ──────────────────────────────────────────────

    /// <summary>Verifies that <c>IncrementBoardViewAsync</c> returns success when no exception.</summary>
    [Fact]
    public async Task IncrementBoardViewAsync_ReturnsSuccess_WhenNoException()
    {
        _boardRepo.Setup(r => r.IncrementViewAsync(1)).Returns(Task.CompletedTask);
        var sut = CreateSut();

        ServiceResult result = await sut.IncrementBoardViewAsync(1);

        Assert.True(result.Success);
        _boardRepo.Verify(r => r.IncrementViewAsync(1), Times.Once);
    }

    /// <summary>Verifies that <c>IncrementBoardViewAsync</c> returns fail when repository throws.</summary>
    [Fact]
    public async Task IncrementBoardViewAsync_ReturnsFail_WhenRepositoryThrows()
    {
        _boardRepo.Setup(r => r.IncrementViewAsync(1)).ThrowsAsync(new Exception("error"));
        var sut = CreateSut();

        ServiceResult result = await sut.IncrementBoardViewAsync(1);

        Assert.False(result.Success);
    }
}
