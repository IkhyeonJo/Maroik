using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Processors;
using Maroik.Website.Grids.Sorts;

namespace Maroik.Website.Tests.Grids.Fixtures;

/// <summary>A hand-written <see cref="IGridSort{T}"/>: the indexer answers <see cref="OnItem"/> (or no sort), and <see cref="Process"/> passes the items through.</summary>
public sealed class FakeGridSort<T> : IGridSort<T>
{
    /// <inheritdoc/>
    public IGrid<T> Grid { get; set; } = null!;

    /// <inheritdoc/>
    public GridProcessorType ProcessorType { get; set; }

    /// <summary>What the indexer answers for a column; <see langword="null"/> answers "not sorted".</summary>
    public Func<IGridColumn<T>, (Int32 Index, GridSortOrder Order)?>? OnItem { get; set; }

    /// <inheritdoc/>
    public (Int32 Index, GridSortOrder Order)? this[IGridColumn<T> column] => OnItem?.Invoke(column);

    /// <inheritdoc/>
    public IQueryable<T> Process(IQueryable<T> items) => items;
}
