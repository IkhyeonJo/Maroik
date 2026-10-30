namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumnFilter<T, TValue> : IGridColumnFilter<T>
{
    IGridColumn<T, TValue> Column { get; set; }
}
