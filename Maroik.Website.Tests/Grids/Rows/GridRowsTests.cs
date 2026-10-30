using System.Collections;
using Maroik.Website.Grids;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Processors;
using Maroik.Website.Grids.Rows;
using Maroik.Website.Tests.Grids.Fixtures;
using Moq;

namespace Maroik.Website.Tests.Grids.Rows;

public class GridRowsTests
{
    [Fact]
    public void GridRows_SetsGrid()
    {
        Grid<GridModel> expected = new([]);
        IGrid<GridModel> actual = new GridRows<GridModel>(expected).Grid;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GetEnumerator_ManuallyProcessesRows()
    {
        IQueryable<GridModel> items = new[] { new GridModel(), new GridModel() }.AsQueryable();
        IGridProcessor<GridModel> postProcessor = Fake.Of<IGridProcessor<GridModel>>();
        IGridProcessor<GridModel> preProcessor = Fake.Of<IGridProcessor<GridModel>>();
        IQueryable<GridModel> postProcessedItems = new[] { new GridModel() }.AsQueryable();
        IQueryable<GridModel> preProcessedItems = new[] { new GridModel() }.AsQueryable();
        Grid<GridModel> grid = new(items) { Mode = GridProcessingMode.Manual };

        Mock.Get(postProcessor).Setup(mock => mock.Process(preProcessedItems)).Returns(postProcessedItems);
        Mock.Get(preProcessor).Setup(mock => mock.Process(items)).Returns(preProcessedItems);
        postProcessor.ProcessorType = GridProcessorType.Post;
        preProcessor.ProcessorType = GridProcessorType.Pre;
        grid.Processors.Add(postProcessor);
        grid.Processors.Add(preProcessor);

        IEnumerable<Object> actual = new GridRows<GridModel>(grid).ToList().Select(row => row.Model);
        IEnumerable<Object> expected = items;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetEnumerator_AutomaticallyProcessesRows()
    {
        IQueryable<GridModel> items = new[] { new GridModel(), new GridModel() }.AsQueryable();
        IGridProcessor<GridModel> postProcessor = Fake.Of<IGridProcessor<GridModel>>();
        IGridProcessor<GridModel> preProcessor = Fake.Of<IGridProcessor<GridModel>>();
        IQueryable<GridModel> postProcessedItems = new[] { new GridModel() }.AsQueryable();
        IQueryable<GridModel> preProcessedItems = new[] { new GridModel() }.AsQueryable();
        Grid<GridModel> grid = new(items) { Mode = GridProcessingMode.Automatic };

        Mock.Get(postProcessor).Setup(mock => mock.Process(preProcessedItems)).Returns(postProcessedItems);
        Mock.Get(preProcessor).Setup(mock => mock.Process(items)).Returns(preProcessedItems);
        postProcessor.ProcessorType = GridProcessorType.Post;
        preProcessor.ProcessorType = GridProcessorType.Pre;
        grid.Processors.Add(postProcessor);
        grid.Processors.Add(preProcessor);

        IEnumerable<Object> actual = new GridRows<GridModel>(grid).ToList().Select(row => row.Model);
        IEnumerable<Object> expected = postProcessedItems;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetEnumerator_SetsRowIndexes()
    {
        IQueryable<GridModel> items = new[] { new GridModel(), new GridModel() }.AsQueryable();
        Grid<GridModel> grid = new(items);
        Int32 index = 0;

        GridRows<GridModel> rows = new(grid);

        Assert.All(rows, row => Assert.Equal(index++, row.Index));
    }

    [Fact]
    public void GetEnumerator_SetsRowAttributes()
    {
        (String key, Object value) = new KeyValuePair<String, Object>("data-id", "1");
        IQueryable<GridModel> items = new[] { new GridModel(), new GridModel() }.AsQueryable();
        GridRows<GridModel> rows = new(new Grid<GridModel>(items)) { Attributes = (_) => new { data_id = "1" } };

        Assert.True(rows.All(row => row.Attributes!.Single().Key == key && row.Attributes!.Single().Value == value));
    }

    [Fact]
    public void GetEnumerator_CachesRows()
    {
        IQueryable<GridModel> items = new[] { new GridModel(), new GridModel() }.AsQueryable();
        IGridProcessor<GridModel> preProcessor = Fake.Of<IGridProcessor<GridModel>>();
        Mock.Get(preProcessor).Setup(mock => mock.Process(items)).Returns(Array.Empty<GridModel>().AsQueryable());
        preProcessor.ProcessorType = GridProcessorType.Pre;
        Grid<GridModel> grid = new(items);

        GridRows<GridModel> rows = new(grid);
        IGridRow<GridModel>[] originalRows = rows.ToArray();

        grid.Processors.Add(preProcessor);

        IEnumerable<Object> actual = rows.ToList().Select(row => row.Model);
        IEnumerable<Object> expected = originalRows.Select(row => row.Model);

        Mock.Get(preProcessor).Verify(mock => mock.Process(It.IsAny<IQueryable<GridModel>>()), Times.Never);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetEnumerator_ReturnsSameEnumerable()
    {
        Grid<GridModel> grid = new(new[] { new GridModel(), new GridModel() });

        GridRows<GridModel> rows = new(grid);

        IEnumerator actual = ((IEnumerable)rows).GetEnumerator();
        IEnumerator expected = rows.GetEnumerator();

        while (expected.MoveNext() | actual.MoveNext())
            Assert.Same(((IGridRow<GridModel>?)expected.Current)?.Model, ((IGridRow<GridModel>?)actual.Current)?.Model);
    }
}
