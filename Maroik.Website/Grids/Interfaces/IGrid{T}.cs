using System.Globalization;

namespace Maroik.Website.Grids.Interfaces;

public interface IGrid<T> : IGrid
{
    IGridSort<T> Sort { get; set; }
    CultureInfo Culture { get; set; }
    IQueryable<T> Source { get; set; }
    HashSet<IGridProcessor<T>> Processors { get; set; }

    new IGridColumnsOf<T> Columns { get; }
    new IGridRowsOf<T> Rows { get; }
    new IGridPager<T>? Pager { get; set; }
}
