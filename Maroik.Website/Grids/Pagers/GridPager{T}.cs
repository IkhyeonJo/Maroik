using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Processors;
using Maroik.Website.Grids.Sorts;

namespace Maroik.Website.Grids.Pagers;

public class GridPager<T> : IGridPager<T>
{
    public IGrid<T> Grid { get; set; }

    public virtual Int32 TotalRows
    {
        get;
        set;
    }
    public virtual Int32 TotalPages
    {
        get
        {
            if (TotalRows == 0)
                return 0;

            if (RowsPerPage == 0)
                return 1;

            return (Int32)Math.Ceiling(TotalRows / (Double)RowsPerPage);
        }
    }
    public virtual Int32 CurrentPage
    {
        get
        {
            String prefix = String.IsNullOrEmpty(Grid.Name) ? "" : Grid.Name + "-";
            CurrentPageValue = Int32.TryParse(Grid.Query?[prefix + "page"], out Int32 page) ? page : CurrentPageValue;
            CurrentPageValue = Math.Min(TotalPages, CurrentPageValue);
            CurrentPageValue = Math.Max(1, CurrentPageValue);

            return CurrentPageValue;
        }
        set
        {
            CurrentPageValue = value;
        }
    }
    public virtual Int32 RowsPerPage
    {
        get
        {
            if (ShowPageSizes)
            {
                String prefix = String.IsNullOrEmpty(Grid.Name) ? "" : Grid.Name + "-";
                RowsPerPageValue = Int32.TryParse(Grid.Query?[prefix + "rows"], out Int32 rows) ? rows : RowsPerPageValue;

                if (PageSizes.Count > 0)
                {
                    if (!PageSizes.ContainsKey(RowsPerPageValue))
                    {
                        IEnumerable<Int32> sizes = PageSizes.Keys.OrderBy(value => value == 0 ? Int32.MaxValue : value);
                        IEnumerable<Int32> higherSizes = sizes.Where(size => RowsPerPageValue < size || size == 0);

                        RowsPerPageValue = higherSizes.Any() ? higherSizes.First() : sizes.Last();
                    }
                }
                else
                {
                    RowsPerPageValue = Math.Max(1, RowsPerPageValue);
                }
            }

            return RowsPerPageValue;
        }
        set
        {
            RowsPerPageValue = value;
        }
    }
    public virtual Int32 PagesToDisplay
    {
        get;
        set;
    }
    public virtual Int32 FirstDisplayPage
    {
        get
        {
            Int32 maxStart = Math.Max(1, TotalPages - PagesToDisplay + 1);
            Int32 middlePage = PagesToDisplay / 2 + PagesToDisplay % 2;
            Int32 firstDisplayPage = CurrentPage - middlePage + 1;

            return Math.Min(maxStart, Math.Max(1, firstDisplayPage));
        }
    }
    public virtual Boolean ShowPageSizes { get; set; }
    public virtual Dictionary<Int32, String> PageSizes { get; set; }

    public String CssClasses { get; set; }
    public String PartialViewName { get; set; }
    public GridProcessorType ProcessorType { get; set; }

    private Int32 CurrentPageValue { get; set; }
    private Int32 RowsPerPageValue { get; set; }

    public GridPager(IGrid<T> grid)
    {
        Grid = grid;
        CssClasses = "";
        CurrentPage = 1;
        RowsPerPage = 20;
        PagesToDisplay = 5;
        PartialViewName = "MvcGrid/_Pager";
        ProcessorType = GridProcessorType.Post;
        PageSizes = new Dictionary<Int32, String> { [10] = "10", [20] = "20", [50] = "50", [100] = "100" };
    }

    public virtual IQueryable<T> Process(IQueryable<T> items)
    {
        TotalRows = items.Count();

        if (RowsPerPage == 0)
            return items;

        if (!GridQuery.IsOrdered(items))
            items = items.OrderBy(_ => 0);

        return items.Skip((CurrentPage - 1) * RowsPerPage).Take(RowsPerPage);
    }
}
