using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Maroik.Website.Tests.Grids.Fixtures;

/// <summary>A hand-written <see cref="IGridColumnFilter{T, TValue}"/>: its properties keep what is assigned, and <see cref="Apply"/> runs <see cref="OnApply"/> (or passes the items through).</summary>
public sealed class FakeGridColumnFilter<T, TValue> : IGridColumnFilter<T, TValue>
{
    /// <inheritdoc/>
    public IGridColumn<T, TValue> Column { get; set; } = null!;

    /// <inheritdoc/>
    public String Name { get; set; } = "";

    /// <inheritdoc/>
    public Boolean? IsEnabled { get; set; }

    /// <inheritdoc/>
    public String DefaultMethod { get; set; } = "";

    /// <inheritdoc/>
    public GridFilterCase? Case { get; set; }

    /// <inheritdoc/>
    public GridFilterType? Type { get; set; }

    /// <inheritdoc/>
    public IEnumerable<SelectListItem> Options { get; set; } = [];

    /// <inheritdoc/>
    public String? Operator { get; set; }

    /// <inheritdoc/>
    public IGridFilter? First { get; set; }

    /// <inheritdoc/>
    public IGridFilter? Second { get; set; }

    /// <summary>What <see cref="Apply"/> answers; <see langword="null"/> passes the items through.</summary>
    public Func<IQueryable<T>, IQueryable<T>>? OnApply { get; set; }

    /// <inheritdoc/>
    public IQueryable<T> Apply(IQueryable<T> items) => OnApply is { } on ? on(items) : items;
}
