using Maroik.Website.Grids.Processors;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridProcessor<T>
{
    GridProcessorType ProcessorType { get; set; }

    IQueryable<T> Process(IQueryable<T> items);
}
