using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Tests.Domains.Board;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Domains.Board.BoardComment"/>.
/// Covers creation validation (writer and content empty guards) and soft-delete behaviour.
/// </summary>
public class BoardCommentTests
{
    // -- Create ---------------------------------------------------------------

    [Fact]
    public void Create_ReturnsComment_WhenValid()
    {
        var result = BoardComment.Create(10, 1, null, "Alice", "Hello!");

        Assert.False(result.IsError);
        Assert.Equal("Alice", result.Value.Writer);
        Assert.Equal("Hello!", result.Value.Content);
    }

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

    [Fact]
    public void SoftDelete_SetsDeletedTrue()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        comment.SoftDelete();

        Assert.True(comment.Deleted);
    }

    // -- IsOwnedBy / CanBeDeletedBy ----------------------------------------------

    [Fact]
    public void IsOwnedBy_ReturnsTrue_ForWriter()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.True(comment.IsOwnedBy("Alice"));
    }

    [Fact]
    public void IsOwnedBy_ReturnsFalse_ForNonWriter()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.False(comment.IsOwnedBy("Bob"));
    }

    [Fact]
    public void CanBeDeletedBy_ReturnsTrue_ForWriter()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.True(comment.CanBeDeletedBy("Alice", isAdmin: false));
    }

    [Fact]
    public void CanBeDeletedBy_ReturnsTrue_ForNonWriterAdmin()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.True(comment.CanBeDeletedBy("Bob", isAdmin: true));
    }

    [Fact]
    public void CanBeDeletedBy_ReturnsFalse_ForNonWriterNonAdmin()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        Assert.False(comment.CanBeDeletedBy("Bob", isAdmin: false));
    }
}
