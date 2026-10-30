using Maroik.Website.Grids.HtmlContents;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridRow<out T>
{
    T Model { get; }
    Int32 Index { get; }

    GridHtmlAttributes? Attributes { get; set; }
}
