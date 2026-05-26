using Maroik.Core.Contract.Domains.Board;

namespace Maroik.Core.Contract.Tests.Domains.Board;

public class BoardCommentTests
{
    // ── Create ───────────────────────────────────────────────────────────────

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

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsDeletedTrue()
    {
        var comment = BoardComment.Create(10, 1, null, "Alice", "Hello!").Value;

        comment.SoftDelete();

        Assert.True(comment.Deleted);
    }
}
