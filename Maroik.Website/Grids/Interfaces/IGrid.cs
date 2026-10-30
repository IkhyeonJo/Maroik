using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.HtmlContents;

namespace Maroik.Website.Grids.Interfaces;

public interface IGrid
{
    String Url { get; set; }
    String? Id { get; set; }
    String Name { get; set; }
    String? EmptyText { get; set; }

    IQueryCollection? Query { get; set; }
    GridProcessingMode Mode { get; set; }
    HttpContext? HttpContext { get; set; }
    GridFilterMode FilterMode { get; set; }
    String FooterPartialViewName { get; set; }
    GridHtmlAttributes Attributes { get; set; }

    IGridColumns<IGridColumn> Columns { get; }
    IGridRows<Object> Rows { get; }
    IGridPager? Pager { get; }
}
