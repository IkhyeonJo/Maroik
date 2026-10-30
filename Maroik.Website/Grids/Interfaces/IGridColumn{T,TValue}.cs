using System.Linq.Expressions;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumn<T, TValue> : IGridProcessor<T>, IGridColumn<T>
{
    Func<T, Int32, Object?>? RenderValue { get; set; }
    Expression<Func<T, TValue>> Expression { get; set; }

    new IGridColumnSort<T, TValue> Sort { get; set; }
    new IGridColumnFilter<T, TValue> Filter { get; set; }
}
