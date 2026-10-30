namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumnSort<T, TValue> : IGridColumnSort<T>
{
    IGridColumn<T, TValue> Column { get; set; }
}
