using Maroik.Core.Domain.Board;

namespace Maroik.Core.DomainTests.BoardDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Board.Board"/>.
/// Covers board creation, update, view incrementing, comment management, file attachment, lock/pin, and soft-delete.
/// </summary>
public class BoardTests
{
    private static Board ValidBoard(long id = 1) =>
        Board.Reconstitute(id, "FreeForum", "Hello World", "content", "Alice", DateTime.UtcNow, DateTime.UtcNow, 0, false, false, false);

    private static BoardComment ValidComment(long boardId = 1) =>
        BoardComment.Reconstitute(1, boardId, 1, null, "Bob", "Nice post!", DateTime.UtcNow, false);

    private static BoardAttachedFile ValidFile(long boardId = 1) =>
        BoardAttachedFile.Create(boardId, 1024, "doc.pdf", ".pdf", "/files/doc.pdf").Value;

    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns board, when valid.</summary>
    [Fact]
    public void Create_ReturnsBoard_WhenValid()
    {
        var result = Board.Create("FreeForum", "Title", "body", "Alice");

        Assert.False(result.IsError);
        Assert.Equal("FreeForum", result.Value.Type);
        Assert.Equal("Alice", result.Value.Writer);
    }

    /// <summary>Create returns error, when type empty.</summary>
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

    /// <summary>Create returns error, when title empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenTitleEmpty(string? title)
    {
        var result = Board.Create("FreeForum", title, null, "Alice");

        Assert.True(result.IsError);
        Assert.Equal("Board.TitleEmpty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when title too long.</summary>
    [Fact]
    public void Create_ReturnsError_WhenTitleTooLong()
    {
        var result = Board.Create("FreeForum", new string('x', 101), null, "Alice");

        Assert.True(result.IsError);
        Assert.Equal("Board.TitleTooLong", result.FirstError.Code);
    }

    /// <summary>Create returns error, when content too long.</summary>
    [Fact]
    public void Create_ReturnsError_WhenContentTooLong()
    {
        var result = Board.Create("FreeForum", "Title", new string('x', 16385), "Alice");

        Assert.True(result.IsError);
        Assert.Equal("Board.ContentTooLong", result.FirstError.Code);
    }

    /// <summary>Create returns error, when writer empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenWriterEmpty(string? writer)
    {
        var result = Board.Create("FreeForum", "Title", null, writer);

        Assert.True(result.IsError);
        Assert.Equal("Board.WriterEmpty", result.FirstError.Code);
    }

    // -- Update ---------------------------------------------------------------

    /// <summary>Update succeeds, when valid.</summary>
    [Fact]
    public void Update_Succeeds_WhenValid()
    {
        var board = ValidBoard();

        var result = board.Update("New Title", "New content");

        Assert.False(result.IsError);
        Assert.Equal("New Title", board.Title);
    }

    /// <summary>Update returns error, when deleted.</summary>
    [Fact]
    public void Update_ReturnsError_WhenDeleted()
    {
        var board = ValidBoard();
        board.SoftDelete();

        var result = board.Update("New Title", null);

        Assert.True(result.IsError);
        Assert.Equal("Board.Deleted", result.FirstError.Code);
    }

    /// <summary>Update returns error, when title too long.</summary>
    [Fact]
    public void Update_ReturnsError_WhenTitleTooLong()
    {
        var board = ValidBoard();

        var result = board.Update(new string('x', 101), null);

        Assert.True(result.IsError);
        Assert.Equal("Board.TitleTooLong", result.FirstError.Code);
    }

    // -- IncrementView ---------------------------------------------------------

    /// <summary>Increment view increments counter.</summary>
    [Fact]
    public void IncrementView_IncrementsCounter()
    {
        var board = ValidBoard();

        board.IncrementView();
        board.IncrementView();

        Assert.Equal(2, board.View);
    }

    // -- AddComment ------------------------------------------------------------

    /// <summary>Add comment succeeds, when not locked or deleted.</summary>
    [Fact]
    public void AddComment_Succeeds_WhenNotLockedOrDeleted()
    {
        var board = ValidBoard(id: 1);
        var comment = ValidComment(boardId: 1);

        var result = board.AddComment(comment);

        Assert.False(result.IsError);
        Assert.Single(board.Comments);
    }

    /// <summary>Add comment returns error, when locked.</summary>
    [Fact]
    public void AddComment_ReturnsError_WhenLocked()
    {
        var board = ValidBoard(id: 1);
        board.Lock();

        var result = board.AddComment(ValidComment(boardId: 1));

        Assert.True(result.IsError);
        Assert.Equal("Board.Locked", result.FirstError.Code);
    }

    /// <summary>Add comment returns error, when locked and commenter is neither the owner nor an admin.</summary>
    [Fact]
    public void AddComment_ReturnsError_WhenLockedAndCommenterIsNotOwnerOrAdmin()
    {
        var board = ValidBoard(id: 1);
        board.Lock();

        var result = board.AddComment(ValidComment(boardId: 1), commenterNickname: "Bob", isAdmin: false);

        Assert.True(result.IsError);
        Assert.Equal("Board.Locked", result.FirstError.Code);
    }

    /// <summary>Add comment succeeds on a locked post, when the commenter is the post's owner.</summary>
    [Fact]
    public void AddComment_Succeeds_WhenLockedAndCommenterIsOwner()
    {
        var board = ValidBoard(id: 1);
        board.Lock();

        var result = board.AddComment(ValidComment(boardId: 1), commenterNickname: "Alice", isAdmin: false);

        Assert.False(result.IsError);
        Assert.Single(board.Comments);
    }

    /// <summary>Add comment succeeds on a locked post, when the commenter is an admin.</summary>
    [Fact]
    public void AddComment_Succeeds_WhenLockedAndCommenterIsAdmin()
    {
        var board = ValidBoard(id: 1);
        board.Lock();

        var result = board.AddComment(ValidComment(boardId: 1), commenterNickname: "Bob", isAdmin: true);

        Assert.False(result.IsError);
        Assert.Single(board.Comments);
    }

    /// <summary>Add comment returns error, when deleted.</summary>
    [Fact]
    public void AddComment_ReturnsError_WhenDeleted()
    {
        var board = ValidBoard(id: 1);
        board.SoftDelete();

        var result = board.AddComment(ValidComment(boardId: 1));

        Assert.True(result.IsError);
        Assert.Equal("Board.Deleted", result.FirstError.Code);
    }

    /// <summary>Add comment returns error, when comment board id mismatch.</summary>
    [Fact]
    public void AddComment_ReturnsError_WhenCommentBoardIdMismatch()
    {
        var board = ValidBoard(id: 1);
        var comment = ValidComment(boardId: 99);

        var result = board.AddComment(comment);

        Assert.True(result.IsError);
        Assert.Equal("Board.CommentMismatch", result.FirstError.Code);
    }

    // -- DeleteComment ---------------------------------------------------------

    /// <summary>Delete comment succeeds, when comment exists.</summary>
    [Fact]
    public void DeleteComment_Succeeds_WhenCommentExists()
    {
        var board = ValidBoard(id: 1);
        board.AddComment(ValidComment(boardId: 1));

        var result = board.DeleteComment(1);

        Assert.False(result.IsError);
        Assert.True(board.Comments[0].Deleted);
    }

    /// <summary>Delete comment returns error, when comment not found.</summary>
    [Fact]
    public void DeleteComment_ReturnsError_WhenCommentNotFound()
    {
        var board = ValidBoard(id: 1);

        var result = board.DeleteComment(999);

        Assert.True(result.IsError);
        Assert.Equal("Board.CommentNotFound", result.FirstError.Code);
    }

    // -- AttachFile ------------------------------------------------------------

    /// <summary>Attach file succeeds, when file matches board.</summary>
    [Fact]
    public void AttachFile_Succeeds_WhenFileMatchesBoard()
    {
        var board = ValidBoard(id: 1);

        var result = board.AttachFile(ValidFile(boardId: 1));

        Assert.False(result.IsError);
        Assert.Single(board.AttachedFiles);
    }

    /// <summary>Attach file returns error, when file board id mismatch.</summary>
    [Fact]
    public void AttachFile_ReturnsError_WhenFileBoardIdMismatch()
    {
        var board = ValidBoard(id: 1);

        var result = board.AttachFile(ValidFile(boardId: 99));

        Assert.True(result.IsError);
        Assert.Equal("Board.FileMismatch", result.FirstError.Code);
    }

    // -- Lock / Unlock ---------------------------------------------------------

    /// <summary>Lock sets locked true.</summary>
    [Fact]
    public void Lock_SetsLockedTrue()
    {
        var board = ValidBoard();

        board.Lock();

        Assert.True(board.Locked);
    }

    /// <summary>Unlock sets locked false.</summary>
    [Fact]
    public void Unlock_SetsLockedFalse()
    {
        var board = ValidBoard();
        board.Lock();

        board.Unlock();

        Assert.False(board.Locked);
    }

    // -- Pin / Unpin -----------------------------------------------------------

    /// <summary>Pin sets noticed true.</summary>
    [Fact]
    public void Pin_SetsNoticedTrue()
    {
        var board = ValidBoard();

        board.Pin();

        Assert.True(board.Noticed);
    }

    /// <summary>Unpin sets noticed false.</summary>
    [Fact]
    public void Unpin_SetsNoticedFalse()
    {
        var board = ValidBoard();
        board.Pin();

        board.Unpin();

        Assert.False(board.Noticed);
    }

    // -- SoftDelete ------------------------------------------------------------

    /// <summary>Soft delete succeeds, when not deleted.</summary>
    [Fact]
    public void SoftDelete_Succeeds_WhenNotDeleted()
    {
        var board = ValidBoard();

        var result = board.SoftDelete();

        Assert.False(result.IsError);
        Assert.True(board.Deleted);
    }

    /// <summary>Soft delete returns error, when already deleted.</summary>
    [Fact]
    public void SoftDelete_ReturnsError_WhenAlreadyDeleted()
    {
        var board = ValidBoard();
        board.SoftDelete();

        var result = board.SoftDelete();

        Assert.True(result.IsError);
        Assert.Equal("Board.AlreadyDeleted", result.FirstError.Code);
    }

    // -- IsOwnedBy / CanBeEditedBy / CanBeDeletedBy -----------------------------

    /// <summary>Is owned by returns true for writer.</summary>
    [Fact]
    public void IsOwnedBy_ReturnsTrue_ForWriter()
    {
        var board = ValidBoard();

        Assert.True(board.IsOwnedBy("Alice"));
    }

    /// <summary>Is owned by returns false for non writer.</summary>
    [Fact]
    public void IsOwnedBy_ReturnsFalse_ForNonWriter()
    {
        var board = ValidBoard();

        Assert.False(board.IsOwnedBy("Bob"));
    }

    /// <summary>Can be edited by returns true for writer.</summary>
    [Fact]
    public void CanBeEditedBy_ReturnsTrue_ForWriter()
    {
        var board = ValidBoard();

        Assert.True(board.CanBeEditedBy("Alice"));
    }

    /// <summary>Can be edited by returns false for admin non writer.</summary>
    [Fact]
    public void CanBeEditedBy_ReturnsFalse_ForAdminNonWriter()
    {
        // Editing is owner-only, unlike deleting — no admin bypass.
        var board = ValidBoard();

        Assert.False(board.CanBeEditedBy("Bob"));
    }

    /// <summary>Can be deleted by returns true for writer.</summary>
    [Fact]
    public void CanBeDeletedBy_ReturnsTrue_ForWriter()
    {
        var board = ValidBoard();

        Assert.True(board.CanBeDeletedBy("Alice", isAdmin: false));
    }

    /// <summary>Can be deleted by returns true for non writer admin.</summary>
    [Fact]
    public void CanBeDeletedBy_ReturnsTrue_ForNonWriterAdmin()
    {
        var board = ValidBoard();

        Assert.True(board.CanBeDeletedBy("Bob", isAdmin: true));
    }

    /// <summary>Can be deleted by returns false for non writer non admin.</summary>
    [Fact]
    public void CanBeDeletedBy_ReturnsFalse_ForNonWriterNonAdmin()
    {
        var board = ValidBoard();

        Assert.False(board.CanBeDeletedBy("Bob", isAdmin: false));
    }
}
