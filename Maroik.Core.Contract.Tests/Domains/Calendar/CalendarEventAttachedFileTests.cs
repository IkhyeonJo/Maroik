using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Tests.Domains.CalendarDomain;

/// <summary>
/// Unit tests for <see cref="CalendarEventAttachedFile"/>.
/// Covers creation validation (name, path, size guards) and reconstitution from trusted data.
/// </summary>
public class CalendarEventAttachedFileTests
{
    // -- Create ---------------------------------------------------------------

    [Fact]
    public void Create_ReturnsFile_WhenValid()
    {
        var result = CalendarEventAttachedFile.Create(1, 512, "agenda.pdf", ".pdf", "/files/agenda.pdf");

        Assert.False(result.IsError);
        Assert.Equal(1, result.Value.CalendarEventId);
        Assert.Equal(512, result.Value.Size);
        Assert.Equal("agenda.pdf", result.Value.Name);
        Assert.Equal(".pdf", result.Value.Extension);
        Assert.Equal("/files/agenda.pdf", result.Value.Path);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenNameEmpty(string? name)
    {
        var result = CalendarEventAttachedFile.Create(1, 512, name, ".pdf", "/files/agenda.pdf");

        Assert.True(result.IsError);
        Assert.Equal("CalendarEventAttachedFile.NameEmpty", result.FirstError.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenPathEmpty(string? path)
    {
        var result = CalendarEventAttachedFile.Create(1, 512, "agenda.pdf", ".pdf", path);

        Assert.True(result.IsError);
        Assert.Equal("CalendarEventAttachedFile.PathEmpty", result.FirstError.Code);
    }

    [Fact]
    public void Create_ReturnsError_WhenSizeNegative()
    {
        var result = CalendarEventAttachedFile.Create(1, -1, "agenda.pdf", ".pdf", "/files/agenda.pdf");

        Assert.True(result.IsError);
        Assert.Equal("CalendarEventAttachedFile.InvalidSize", result.FirstError.Code);
    }

    // -- Reconstitute -----------------------------------------------------------

    [Fact]
    public void Reconstitute_RebuildsFile_WithoutValidation()
    {
        var file = CalendarEventAttachedFile.Reconstitute(5, 1, 2048, "slides.pptx", ".pptx", "/files/slides.pptx");

        Assert.Equal(5, file.Id);
        Assert.Equal(1, file.CalendarEventId);
        Assert.Equal(2048, file.Size);
        Assert.Equal("slides.pptx", file.Name);
        Assert.Equal(".pptx", file.Extension);
        Assert.Equal("/files/slides.pptx", file.Path);
    }

    [Fact]
    public void Reconstitute_DefaultsNullNameAndPath_ToEmptyString()
    {
        var file = CalendarEventAttachedFile.Reconstitute(1, 1, 0, null, null, null);

        Assert.Equal(string.Empty, file.Name);
        Assert.Equal(string.Empty, file.Path);
        Assert.Null(file.Extension);
    }
}
