using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.DomainTests.ValueObjects;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Domain.ValueObjects.HtmlColorCode"/>.
/// Covers creation with valid 3- and 6-digit hex codes, upper-case normalisation, empty/invalid format rejections, and equality.
/// </summary>
public class HtmlColorCodeTests
{
    /// <summary>Create returns code and normalizes to upper case.</summary>
    [Theory]
    [InlineData("#FF5733", "#FF5733")]
    [InlineData("#ff5733", "#FF5733")]
    [InlineData("#ABC", "#ABC")]
    [InlineData("#abc", "#ABC")]
    public void Create_ReturnsCode_AndNormalizesToUpperCase(string input, string expected)
    {
        var result = HtmlColorCode.Create(input);

        Assert.False(result.IsError);
        Assert.Equal(expected, result.Value.Value);
    }

    /// <summary>Create returns error, when empty.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ReturnsError_WhenEmpty(string? input)
    {
        var result = HtmlColorCode.Create(input);

        Assert.True(result.IsError);
        Assert.Equal("HtmlColorCode.Empty", result.FirstError.Code);
    }

    /// <summary>Create returns error, when invalid format.</summary>
    [Theory]
    [InlineData("FF5733")]
    [InlineData("#GGGGGG")]
    [InlineData("#12345")]
    [InlineData("#1234567")]
    [InlineData("red")]
    public void Create_ReturnsError_WhenInvalidFormat(string input)
    {
        var result = HtmlColorCode.Create(input);

        Assert.True(result.IsError);
        Assert.Equal("HtmlColorCode.Invalid", result.FirstError.Code);
    }

    /// <summary>
    /// The "invalid format" error is built via <c>LocalizableError</c>: its Metadata carries the
    /// composite-format resource template and the raw offending value separately, so
    /// <c>ServiceResult.FromError</c> can hand the UI something resx-localizable instead of the
    /// already-baked, value-embedding sentence in <see cref="ErrorOr.Error.Description"/>.
    /// </summary>
    [Fact]
    public void Create_ReturnsError_WithLocalizableMetadata_WhenInvalidFormat()
    {
        var result = HtmlColorCode.Create("red");

        Assert.Equal("'red' is not a valid HTML hex color code (e.g. #FF5733).", result.FirstError.Description);
        Assert.Equal("'{0}' is not a valid HTML hex color code (e.g. #FF5733).", result.FirstError.Metadata!["ResourceKey"]);
        Assert.Equal(["red"], (object[])result.FirstError.Metadata["ResourceArgs"]);
    }

    /// <summary>Equality same value are equal.</summary>
    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var a = HtmlColorCode.Create("#FF0000").Value;
        var b = HtmlColorCode.Create("#ff0000").Value;

        Assert.Equal(a, b);
    }
}
