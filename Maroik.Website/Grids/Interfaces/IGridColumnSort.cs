using Maroik.Website.Grids.Sorts;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumnSort
{
    Int32? Index { get; }
    GridSortOrder? Order { get; }
    Boolean? IsEnabled { get; set; }
    GridSortOrder FirstOrder { get; set; }
}
