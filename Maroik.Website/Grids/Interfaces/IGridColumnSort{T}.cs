namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumnSort<T> : IGridColumnSort
{
    IQueryable<T> By(IQueryable<T> items);
    IQueryable<T> ThenBy(IOrderedQueryable<T> items);
}
