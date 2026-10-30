using System.Text.Encodings.Web;
using Maroik.Website.Grids.HtmlContents;

namespace Maroik.Website.Tests.Grids.HtmlContents;

public sealed class GridHtmlStringTests : IDisposable
{
    private StringWriter writer;

    public GridHtmlStringTests()
    {
        writer = new StringWriter();
    }
    public void Dispose()
    {
        writer.Dispose();
    }

    [Fact]
    public void WriteTo_RawString()
    {
        new GridHtmlString("<test>").WriteTo(writer, null);

        Assert.Equal("<test>", writer.ToString());
    }

    [Fact]
    public void WriteTo_EncodedString()
    {
        new GridHtmlString("<test>").WriteTo(writer, HtmlEncoder.Default);

        Assert.Equal(HtmlEncoder.Default.Encode("<test>"), writer.ToString());
    }

    [Theory]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    [InlineData(null, "")]
    [InlineData("test", "test")]
    public void ToString_Value(String? value, String expected)
    {
        Assert.Equal(expected, new GridHtmlString(value).ToString());
    }
}
