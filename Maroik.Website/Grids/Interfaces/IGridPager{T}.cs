namespace Maroik.Website.Grids.Interfaces;

public interface IGridPager<T> : IGridProcessor<T>, IGridPager
{
    IGrid<T> Grid { get; }
}
