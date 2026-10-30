using Maroik.Website.Grids.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Tests.Grids.Fixtures;

/// <summary>
/// A hand-written <see cref="IGridFilters"/>: <see cref="Create"/> runs <see cref="OnCreate"/> and <see cref="OptionsFor{T, TValue}"/>
/// <see cref="OnOptionsFor"/> (given the column), each recording its arguments; without a behaviour they answer no filter / no options.
/// </summary>
public sealed class FakeGridFilters : IGridFilters
{
    /// <inheritdoc/>
    public Func<String> BooleanTrueOptionText { get; set; } = () => "";

    /// <inheritdoc/>
    public Func<String> BooleanFalseOptionText { get; set; } = () => "";

    /// <inheritdoc/>
    public Func<String> BooleanEmptyOptionText { get; set; } = () => "";

    /// <summary>What <see cref="Create"/> answers.</summary>
    public Func<Type, String, StringValues, IGridFilter?>? OnCreate { get; set; }

    /// <summary>The arguments of every <see cref="Create"/> call, in order.</summary>
    public List<(Type Type, String Method, StringValues Values)> CreateCalls { get; } = [];

    /// <summary>What <see cref="OptionsFor{T, TValue}"/> answers for a column.</summary>
    public Func<Object, IEnumerable<SelectListItem>>? OnOptionsFor { get; set; }

    /// <summary>The column of every <see cref="OptionsFor{T, TValue}"/> call, in order.</summary>
    public List<Object> OptionsForCalls { get; } = [];

    /// <inheritdoc/>
    public IGridFilter? Create(Type type, String method, StringValues values)
    {
        CreateCalls.Add((type, method, values));
        return OnCreate?.Invoke(type, method, values);
    }

    /// <inheritdoc/>
    public IEnumerable<SelectListItem> OptionsFor<T, TValue>(IGridColumn<T, TValue> column)
    {
        OptionsForCalls.Add(column);
        return OnOptionsFor?.Invoke(column) ?? [];
    }

    /// <inheritdoc/>
    public void Register(Type type, String method, Type filter) => throw new NotSupportedException("Register is not faked.");

    /// <inheritdoc/>
    public void Unregister(Type type, String method) => throw new NotSupportedException("Unregister is not faked.");
}
