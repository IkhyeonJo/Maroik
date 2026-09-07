using Maroik.Core.Domain.Board;

namespace Maroik.Core.DomainTests.BoardDomain;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.Board.BoardComment"/>.
/// Covers creation validation (writer and content empty guards) and soft-delete behaviour.
/// </summary>
public class BoardCommentTests
{
    // -- Create ---------------------------------------------------------------

    /// <summary>Create returns comment, when valid.</summary>
    [Fact]
    public void Create_ReturnsComment_WhenValid()
    {
        var result = BoardComment.Create(10, 1, null, "Alice", "Hello!");

        Assert.False(result.IsError);
        Assert.Equal("Alice", result.Value.Writer);
        Assert.Equal("Hello!", result.Value.Content);
    }

    /// <summary>Create returns error, when writer empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenWriterEmpty(string? writer)
    {
        var result = BoardComment.Create(10, 1, null, writer, "content");

        Assert.True(result.IsError);
        Assert.Equal("BoardComment.WriterEmpty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when content empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenContentEmpty(string? content)
    {
        var result = BoardComment.Create(10, 1, null, "Alice", content);

        Assert.True(result.IsError);
        Assert.Equal("BoardComment.ContentEmpty", result.FirstError.Code);
    }

    // -- SoftDelete ------------------------------------------------------------

    /// <summary>Soft delete succeeds, when not deleted.</summary>
    [Fact]
    public void SoftDelete_Succeeds_WhenNotDeleted()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        var result = comment.SoftDelete();

        Assert.False(result.IsError);
        Assert.True(comment.Deleted);
    }

    /// <summary>Soft delete returns error, when already deleted.</summary>
    [Fact]
    public void SoftDelete_ReturnsError_WhenAlreadyDeleted()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;
        comment.SoftDelete();

        var result = comment.SoftDelete();

        Assert.True(result.IsError);
        Assert.Equal("BoardComment.AlreadyDeleted", result.FirstError.Code);
    }

    // -- IsOwnedBy / CanBeDeletedBy ----------------------------------------------

    /// <summary>Is owned by returns true for writer.</summary>
    [Fact]
    public void IsOwnedBy_ReturnsTrue_ForWriter()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.True(comment.IsOwnedBy("Alice"));
    }

    /// <summary>Is owned by returns false for non writer.</summary>
    [Fact]
    public void IsOwnedBy_ReturnsFalse_ForNonWriter()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.False(comment.IsOwnedBy("Bob"));
    }

    /// <summary>Can be deleted by returns true for writer.</summary>
    [Fact]
    public void CanBeDeletedBy_ReturnsTrue_ForWriter()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.True(comment.CanBeDeletedBy("Alice", isAdmin: false));
    }

    /// <summary>Can be deleted by returns true for non writer admin.</summary>
    [Fact]
    public void CanBeDeletedBy_ReturnsTrue_ForNonWriterAdmin()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.True(comment.CanBeDeletedBy("Bob", isAdmin: true));
    }

    /// <summary>Can be deleted by returns false for non writer non admin.</summary>
    [Fact]
    public void CanBeDeletedBy_ReturnsFalse_ForNonWriterNonAdmin()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.False(comment.CanBeDeletedBy("Bob", isAdmin: false));
    }
}
