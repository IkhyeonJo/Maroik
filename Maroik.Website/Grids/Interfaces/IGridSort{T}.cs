using Maroik.Website.Grids.Sorts;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridSort<T> : IGridProcessor<T>
{
    IGrid<T> Grid { get; set; }

    (Int32 Index, GridSortOrder Order)? this[IGridColumn<T> column] { get; }
}
