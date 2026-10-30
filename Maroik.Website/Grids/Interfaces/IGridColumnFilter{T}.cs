namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumnFilter<T> : IGridColumnFilter
{
    IQueryable<T> Apply(IQueryable<T> items);
}
