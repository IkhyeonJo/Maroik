using Maroik.Core.Contract.Domains.Board;
using Maroik.Core.Contract.Utilities;

namespace Maroik.Core.Contract.Tests.Domain.Post;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Domains.Board.Board"/>.
/// Covers board creation, update, view incrementing, comment management, file attachment, lock/pin, and soft-delete.
/// </summary>
public class BoardTests
{
    private static Board ValidBoard(long id = 1) =>
        Board.Reconstitute(id, nameof(BoardType.FreeForum), "Hello World", "content", "Alice", DateTime.UtcNow, DateTime.UtcNow, 0, false, false, false);

    private static BoardComment ValidComment(long boardId = 1) =>
        BoardComment.Reconstitute(1, boardId, 1, null, "Bob", "Nice post!", DateTime.UtcNow, false);

    private static BoardAttachedFile ValidFile(long boardId = 1) =>
        BoardAttachedFile.Create(boardId, 1024, "doc.pdf", ".pdf", "/files/doc.pdf").Value;

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ReturnsBoard_WhenValid()
    {
        var result = Board.Create(nameof(BoardType.FreeForum), "Title", "body", "Alice");

        Assert.False(result.IsError);
        Assert.Equal(nameof(BoardType.FreeForum), result.Value.Type);
        Assert.Equal("Alice", result.Value.Writer);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenTypeEmpty(string? type)
    {
        var result = Board.Create(type, "Title", null, "Alice");

        Assert.True(result.IsError);
        Assert.Equal("Board.TypeEmpty", result.FirstError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenTitleEmpty(string? title)
    {
        var result = Board.Create(nameof(BoardType.FreeForum), title, null, "Alice");

        Assert.True(result.IsError);
        Assert.Equal("Board.TitleEmpty", result.FirstError.Code);
    }

    [Fact]
    public void Create_ReturnsError_WhenTitleTooLong()
    {
        var result = Board.Create(nameof(BoardType.FreeForum), new string('x', 101), null, "Alice");

        Assert.True(result.IsError);
        Assert.Equal("Board.TitleTooLong", result.FirstError.Code);
    }

    [Fact]
    public void Create_ReturnsError_WhenContentTooLong()
    {
        var result = Board.Create(nameof(BoardType.FreeForum), "Title", new string('x', 16385), "Alice");

        Assert.True(result.IsError);
        Assert.Equal("Board.ContentTooLong", result.FirstError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenWriterEmpty(string? writer)
    {
        var result = Board.Create(nameof(BoardType.FreeForum), "Title", null, writer);

        Assert.True(result.IsError);
        Assert.Equal("Board.WriterEmpty", result.FirstError.Code);
    }

    // ── Update ───────────────────────────────────────────────────────────────

    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var board = ValidBoard();

        var result = board.Update("New Title", "New content");

        Assert.False(result.IsError);
        Assert.Equal("New Title", board.Title);
    }

    [Fact]
    public void Update_ReturnsError_WhenDeleted()
    {
        var board = ValidBoard();
        board.SoftDelete();

        var result = board.Update("New Title", null);

        Assert.True(result.IsError);
        Assert.Equal("Board.Deleted", result.FirstError.Code);
    }

    [Fact]
    public void Update_ReturnsError_WhenTitleTooLong()
    {
        var board = ValidBoard();

        var result = board.Update(new string('x', 101), null);

        Assert.True(result.IsError);
        Assert.Equal("Board.TitleTooLong", result.FirstError.Code);
    }

    // ── IncrementView ─────────────────────────────────────────────────────────

    [Fact]
    public void IncrementView_IncrementsCounter()
    {
        var board = ValidBoard();

        board.IncrementView();
        board.IncrementView();

        Assert.Equal(2, board.View);
    }

    // ── AddComment ────────────────────────────────────────────────────────────

    [Fact]
    public void AddComment_Succeeds_WhenNotLockedOrDeleted()
    {
        var board = ValidBoard(id: 1);
        var comment = ValidComment(boardId: 1);

        var result = board.AddComment(comment);

        Assert.False(result.IsError);
        Assert.Single(board.Comments);
    }

    [Fact]
    public void AddComment_ReturnsError_WhenLocked()
    {
        var board = ValidBoard(id: 1);
        board.Lock();

        var result = board.AddComment(ValidComment(boardId: 1));

        Assert.True(result.IsError);
        Assert.Equal("Board.Locked", result.FirstError.Code);
    }

    [Fact]
    public void AddComment_ReturnsError_WhenDeleted()
    {
        var board = ValidBoard(id: 1);
        board.SoftDelete();

        var result = board.AddComment(ValidComment(boardId: 1));

        Assert.True(result.IsError);
        Assert.Equal("Board.Deleted", result.FirstError.Code);
    }

    [Fact]
    public void AddComment_ReturnsError_WhenCommentBoardIdMismatch()
    {
        var board = ValidBoard(id: 1);
        var comment = ValidComment(boardId: 99);

        var result = board.AddComment(comment);

        Assert.True(result.IsError);
        Assert.Equal("Board.CommentMismatch", result.FirstError.Code);
    }

    // ── DeleteComment ─────────────────────────────────────────────────────────

    [Fact]
    public void DeleteComment_Succeeds_WhenCommentExists()
    {
        var board = ValidBoard(id: 1);
        board.AddComment(ValidComment(boardId: 1));

        var result = board.DeleteComment(1);

        Assert.False(result.IsError);
        Assert.True(board.Comments[0].Deleted);
    }

    [Fact]
    public void DeleteComment_ReturnsError_WhenCommentNotFound()
    {
        var board = ValidBoard(id: 1);

        var result = board.DeleteComment(999);

        Assert.True(result.IsError);
        Assert.Equal("Board.CommentNotFound", result.FirstError.Code);
    }

    // ── AttachFile ────────────────────────────────────────────────────────────

    [Fact]
    public void AttachFile_Succeeds_WhenFileMatchesBoard()
    {
        var board = ValidBoard(id: 1);

        var result = board.AttachFile(ValidFile(boardId: 1));

        Assert.False(result.IsError);
        Assert.Single(board.AttachedFiles);
    }

    [Fact]
    public void AttachFile_ReturnsError_WhenFileBoardIdMismatch()
    {
        var board = ValidBoard(id: 1);

        var result = board.AttachFile(ValidFile(boardId: 99));

        Assert.True(result.IsError);
        Assert.Equal("Board.FileMismatch", result.FirstError.Code);
    }

    // ── Lock / Unlock ─────────────────────────────────────────────────────────

    [Fact]
    public void Lock_SetsLockedTrue()
    {
        var board = ValidBoard();

        board.Lock();

        Assert.True(board.Locked);
    }

    [Fact]
    public void Unlock_SetsLockedFalse()
    {
        var board = ValidBoard();
        board.Lock();

        board.Unlock();

        Assert.False(board.Locked);
    }

    // ── Pin / Unpin ───────────────────────────────────────────────────────────

    [Fact]
    public void Pin_SetsNoticedTrue()
    {
        var board = ValidBoard();

        board.Pin();

        Assert.True(board.Noticed);
    }

    [Fact]
    public void Unpin_SetsNoticedFalse()
    {
        var board = ValidBoard();
        board.Pin();

        board.Unpin();

        Assert.False(board.Noticed);
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_Succeeds_WhenNotDeleted()
    {
        var board = ValidBoard();

        var result = board.SoftDelete();

        Assert.False(result.IsError);
        Assert.True(board.Deleted);
    }

    [Fact]
    public void SoftDelete_ReturnsError_WhenAlreadyDeleted()
    {
        var board = ValidBoard();
        board.SoftDelete();

        var result = board.SoftDelete();

        Assert.True(result.IsError);
        Assert.Equal("Board.AlreadyDeleted", result.FirstError.Code);
    }
}
