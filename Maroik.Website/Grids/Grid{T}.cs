using System.Globalization;
using Maroik.Website.Grids.Columns;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.HtmlContents;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Rows;
using Maroik.Website.Grids.Sorts;

namespace Maroik.Website.Grids;

public class Grid<T> : IGrid<T> where T : class
{
    public String Url { get; set; }
    public String? Id { get; set; }
    public String Name { get; set; }
    public String? EmptyText { get; set; }

    public IGridSort<T> Sort { get; set; }
    public CultureInfo Culture { get; set; }
    public IQueryable<T> Source { get; set; }
    public IQueryCollection? Query { get; set; }
    public GridProcessingMode Mode { get; set; }
    public HttpContext? HttpContext { get; set; }
    public GridFilterMode FilterMode { get; set; }
    public String FooterPartialViewName { get; set; }
    public GridHtmlAttributes Attributes { get; set; }
    public HashSet<IGridProcessor<T>> Processors { get; set; }

    IGridColumns<IGridColumn> IGrid.Columns => Columns;
    public IGridColumnsOf<T> Columns { get; set; }

    IGridRows<Object> IGrid.Rows => Rows;
    public IGridRowsOf<T> Rows { get; set; }

    IGridPager? IGrid.Pager => Pager;
    public IGridPager<T>? Pager { get; set; }

    public Grid(IEnumerable<T> source)
        : this(source, CultureInfo.CurrentCulture)
    {
    }
    public Grid(IEnumerable<T> source, CultureInfo culture)
    {
        Url = "";
        Name = "";
        Culture = culture;
        FooterPartialViewName = "";
        Source = source.AsQueryable();
        FilterMode = GridFilterMode.Excel;
        Mode = GridProcessingMode.Automatic;
        Attributes = new GridHtmlAttributes();
        Processors = new HashSet<IGridProcessor<T>>();

        Columns = new GridColumns<T>(this);
        Rows = new GridRows<T>(this);
        Sort = new GridSort<T>(this);
    }
}
