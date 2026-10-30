namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumns<out T> : IEnumerable<T> where T : IGridColumn
{
}
