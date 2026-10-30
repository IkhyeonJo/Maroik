using Maroik.Website.Grids.HtmlContents;
using Maroik.Website.Grids.Interfaces;

namespace Maroik.Website.Grids.Rows;

public class GridRow<T> : IGridRow<T>
{
    public T Model { get; }
    public Int32 Index { get; }

    public GridHtmlAttributes? Attributes { get; set; }

    public GridRow(T model, Int32 index)
    {
        Index = index;
        Model = model;
    }
}
