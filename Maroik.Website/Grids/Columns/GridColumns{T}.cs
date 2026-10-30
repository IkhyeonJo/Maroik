using System.Linq.Expressions;
using Maroik.Website.Grids.Interfaces;

namespace Maroik.Website.Grids.Columns;

public class GridColumns<T> : List<IGridColumn<T>>, IGridColumnsOf<T> where T : class
{
    public IGrid<T> Grid { get; set; }

    public GridColumns(IGrid<T> grid)
    {
        Grid = grid;
    }

    public virtual IGridColumn<T, Object> Add()
    {
        return Add<Object>(_ => "");
    }
    public virtual IGridColumn<T, TValue> Add<TValue>(Expression<Func<T, TValue>> expression)
    {
        GridColumn<T, TValue> column = new(Grid, expression);
        Grid.Processors.Add(column);
        Add(column);

        return column;
    }

    public virtual IGridColumn<T, Object> Insert(Int32 index)
    {
        return Insert<Object>(index, _ => "");
    }
    public virtual IGridColumn<T, TValue> Insert<TValue>(Int32 index, Expression<Func<T, TValue>> expression)
    {
        GridColumn<T, TValue> column = new(Grid, expression);
        Grid.Processors.Add(column);
        Insert(index, column);

        return column;
    }
}
