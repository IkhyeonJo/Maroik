using Microsoft.AspNetCore.Html;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumn
{
    String Name { get; set; }
    String Style { get; set; }
    Object Title { get; set; }
    String? Format { get; set; }
    Boolean IsHidden { get; set; }
    String CssClasses { get; set; }
    Boolean IsEncoded { get; set; }

    IGridColumnSort Sort { get; }
    IGridColumnFilter Filter { get; }

    IHtmlContent ValueFor(IGridRow<Object> row);
}
