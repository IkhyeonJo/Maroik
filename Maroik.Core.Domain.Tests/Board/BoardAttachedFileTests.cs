using Maroik.Core.Domain.Board;

namespace Maroik.Core.DomainTests.BoardDomain;

/// <summary>
/// Unit tests for <see cref="BoardAttachedFile"/>.
/// Covers creation validation (name, path, size guards) and reconstitution from trusted data.
/// </summary>
public class BoardAttachedFileTests
{
    // -- Create ---------------------------------------------------------------

    [Fact]
    public void Create_ReturnsFile_WhenValid()
    {
        var result = BoardAttachedFile.Create(1, 1024, "doc.pdf", ".pdf", "/files/doc.pdf");

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.BoardId);
        Assert.Equal(1024, result.Value.Size);
        Assert.Equal("doc.pdf", result.Value.Name);
        Assert.Equal(".pdf", result.Value.Extension);
        Assert.Equal("/files/doc.pdf", result.Value.Path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenNameEmpty(string? name)
    {
        var result = BoardAttachedFile.Create(1, 1024, name, ".pdf", "/files/doc.pdf");

        Assert.True(result.IsError);
        Assert.Equal("BoardAttachedFile.NameEmpty", result.FirstError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenPathEmpty(string? path)
    {
        var result = BoardAttachedFile.Create(1, 1024, "doc.pdf", ".pdf", path);

        Assert.True(result.IsError);
        Assert.Equal("BoardAttachedFile.PathEmpty", result.FirstError.Code);
    }

    [Fact]
    public void Create_ReturnsError_WhenSizeNegative()
    {
        var result = BoardAttachedFile.Create(1, -1, "doc.pdf", ".pdf", "/files/doc.pdf");

        Assert.True(result.IsError);
        Assert.Equal("BoardAttachedFile.InvalidSize", result.FirstError.Code);
    }

    // -- Reconstitute -----------------------------------------------------------

    [Fact]
    public void Reconstitute_RebuildsFile_WithoutValidation()
    {
        var file = BoardAttachedFile.Reconstitute(5, 1, 2048, "image.png", ".png", "/files/image.png");

        Assert.Equal(5, file.Id);
        Assert.Equal(1, file.BoardId);
        Assert.Equal(2048, file.Size);
        Assert.Equal("image.png", file.Name);
        Assert.Equal(".png", file.Extension);
        Assert.Equal("/files/image.png", file.Path);
    }

    [Fact]
    public void Reconstitute_DefaultsNullNameAndPath_ToEmptyString()
    {
        var file = BoardAttachedFile.Reconstitute(1, 1, 0, null, null, null);

        Assert.Equal("", file.Name);
        Assert.Equal("", file.Path);
        Assert.Null(file.Extension);
    }
}
