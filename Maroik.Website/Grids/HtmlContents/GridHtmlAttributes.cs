using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Maroik.Website.Grids.HtmlContents;

public class GridHtmlAttributes : Dictionary<String, Object?>, IHtmlContent
{
    public GridHtmlAttributes()
    {
    }
    public GridHtmlAttributes(Object? attributes)
        : base(HtmlHelper.AnonymousObjectToHtmlAttributes(attributes))
    {
    }

    public void WriteTo(TextWriter writer, HtmlEncoder encoder)
    {
        foreach ((String attribute, Object? rawValue) in this)
        {
            String? value = rawValue?.ToString();

            if (value == null)
                continue;

            writer.Write(" ");
            writer.Write(attribute);
            writer.Write("=\"");

            writer.Write(encoder.Encode(value));

            writer.Write("\"");
        }
    }
}
