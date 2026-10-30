using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.Interfaces;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Tests.Grids.Fixtures;

/// <summary>A hand-written <see cref="IGridFilter"/>: its properties keep what is assigned, and <see cref="Apply"/> runs <see cref="OnApply"/>.</summary>
public sealed class FakeGridFilter : IGridFilter
{
    /// <inheritdoc/>
    public String? Method { get; set; }

    /// <inheritdoc/>
    public StringValues Values { get; set; }

    /// <inheritdoc/>
    public GridFilterCase Case { get; set; }

    /// <summary>What <see cref="Apply"/> answers; <see langword="null"/> answers no filter (<see langword="null"/>).</summary>
    public Func<Expression, CultureInfo, Expression?>? OnApply { get; set; }

    /// <inheritdoc/>
    public Expression? Apply(Expression expression, CultureInfo culture) => OnApply?.Invoke(expression, culture);
}
