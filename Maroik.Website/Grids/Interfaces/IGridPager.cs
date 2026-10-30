namespace Maroik.Website.Grids.Interfaces;

public interface IGridPager
{
    Int32 TotalPages { get; }
    Int32 TotalRows { get; set; }
    Int32 CurrentPage { get; set; }
    Int32 RowsPerPage { get; set; }
    Int32 FirstDisplayPage { get; }
    Int32 PagesToDisplay { get; set; }
    Boolean ShowPageSizes { get; set; }
    Dictionary<Int32, String> PageSizes { get; set; }

    String CssClasses { get; set; }
    String PartialViewName { get; set; }
}
