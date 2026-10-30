using System.Linq.Expressions;
using Maroik.Website.Grids.Interfaces;

namespace Maroik.Website.Grids.Sorts;

public class GridColumnSort<T, TValue> : IGridColumnSort<T, TValue>
{
    public virtual Int32? Index
    {
        get
        {
            return Column.Grid.Sort[Column]?.Index;
        }
    }
    public virtual GridSortOrder? Order
    {
        get
        {
            return Column.Grid.Sort[Column]?.Order;
        }
    }
    public Boolean? IsEnabled { get; set; }
    public GridSortOrder FirstOrder { get; set; }

    public IGridColumn<T, TValue> Column { get; set; }

    public GridColumnSort(IGridColumn<T, TValue> column)
    {
        Column = column;
        FirstOrder = GridSortOrder.Asc;
        IsEnabled = column.Expression.Body is MemberExpression ? IsEnabled : false;
    }

    public IQueryable<T> By(IQueryable<T> items)
    {
        if (IsEnabled != true)
            return items;

        return Order == GridSortOrder.Asc ? items.OrderBy(Column.Expression) : items.OrderByDescending(Column.Expression);
    }
    public IQueryable<T> ThenBy(IOrderedQueryable<T> items)
    {
        if (IsEnabled != true)
            return items;

        return Order == GridSortOrder.Asc ? items.ThenBy(Column.Expression) : items.ThenByDescending(Column.Expression);
    }
}
