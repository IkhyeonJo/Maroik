using System.Text;
using Maroik.Website.Extensions;
using Microsoft.AspNetCore.Http;

namespace Maroik.Website.Tests.Extensions;

/// <summary>Unit tests for <see cref="FormFileExtensions"/>.</summary>
public class FormFileExtensionsTests
{
    private static IFormFile MakeFormFile(string content, string fileName, string contentType)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "file", fileName) { Headers = new HeaderDictionary(), ContentType = contentType };
    }

    /// <summary>To attached file info async null file returns null.</summary>
    [Fact]
    public async Task ToAttachedFileInfoAsync_NullFile_ReturnsNull()
    {
        IFormFile? file = null;

        var result = await file.ToAttachedFileInfoAsync(TestContext.Current.CancellationToken);

        Assert.Null(result);
    }

    /// <summary>To attached file info async reads bytes content type file name and size.</summary>
    [Fact]
    public async Task ToAttachedFileInfoAsync_ReadsBytesContentTypeFileNameAndSize()
    {
        IFormFile file = MakeFormFile("hello world", "note.txt", "text/plain");

        var result = await file.ToAttachedFileInfoAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("hello world", Encoding.UTF8.GetString(result.Bytes));
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal("note.txt", result.FileName);
        Assert.Equal(11, result.Size);
    }
}
