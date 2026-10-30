using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Processors;

namespace Maroik.Website.Tests.Grids.Fixtures;

/// <summary>A hand-written <see cref="IGridProcessor{T}"/>: <see cref="Process"/> runs <see cref="OnProcess"/> (or passes the items through) and records each call.</summary>
public sealed class FakeGridProcessor<T> : IGridProcessor<T>
{
    /// <inheritdoc/>
    public GridProcessorType ProcessorType { get; set; }

    /// <summary>What <see cref="Process"/> answers; <see langword="null"/> passes the items through.</summary>
    public Func<IQueryable<T>, IQueryable<T>>? OnProcess { get; set; }

    /// <summary>The items of every <see cref="Process"/> call, in order.</summary>
    public List<IQueryable<T>> ProcessCalls { get; } = [];

    /// <inheritdoc/>
    public IQueryable<T> Process(IQueryable<T> items)
    {
        ProcessCalls.Add(items);
        return OnProcess is { } on ? on(items) : items;
    }
}
