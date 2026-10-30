using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;

namespace Maroik.Website.Grids.HtmlContents;

public class GridHtmlString : IHtmlContent
{
    private String Value { get; }

    public GridHtmlString(String? value)
    {
        Value = value ?? "";
    }

    public void WriteTo(TextWriter writer, HtmlEncoder? encoder)
    {
        writer.Write(encoder?.Encode(Value) ?? Value);
    }

    public override String ToString()
    {
        return Value;
    }
}
