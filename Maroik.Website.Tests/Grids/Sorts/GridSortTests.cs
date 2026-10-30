using Maroik.Website.Grids;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Processors;
using Maroik.Website.Grids.Sorts;
using Maroik.Website.Tests.Grids.Fixtures;

namespace Maroik.Website.Tests.Grids.Sorts;

public class GridSortTests
{
    private GridSort<GridModel> sort;
    private IGridColumn<GridModel, Int32> sumColumn;
    private IGridColumn<GridModel, String?> nameColumn;
    private IGridColumn<GridModel, String?> textColumn;

    public GridSortTests()
    {
        Grid<GridModel> grid = new([]);
        sort = new GridSort<GridModel>(grid);
        sumColumn = grid.Columns.Add(model => model.Sum);
        nameColumn = grid.Columns.Add(model => model.Name);
        textColumn = grid.Columns.Add(model => model.Text);
    }

    [Fact]
    public void GridSort_SetsGrid()
    {
        Object expected = sort.Grid;
        Object actual = new GridSort<GridModel>(sort.Grid).Grid;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GridSort_SetsProcessorType()
    {
        GridProcessorType expected = new GridSort<GridModel>(sort.Grid).ProcessorType;
        GridProcessorType actual = GridProcessorType.Pre;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Indexer_DisabledSort_ReturnsNull()
    {
        sumColumn.Name = "sum";
        sumColumn.Sort.IsEnabled = false;
        sumColumn.Grid.Query = HttpUtility.ParseQueryString("sort=sum asc");

        Assert.Null(sort[sumColumn]);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("", "sort=")]
    [InlineData("", "sort=asc")]
    [InlineData("", "sort=sum as")]
    [InlineData("", "sort=sumasc")]
    [InlineData("", "sort=sum-asc")]
    [InlineData("", "sort=name asc")]
    [InlineData("", "sort=grid-sum-asc")]
    [InlineData("grid", "")]
    [InlineData("grid", "grid-sort=")]
    [InlineData("grid", "grid-sort=asc")]
    [InlineData("grid", "grid-sort=sum as")]
    [InlineData("grid", "grid-sort=sumasc")]
    [InlineData("grid", "grid-sort=sum-asc")]
    [InlineData("grid", "grid-sort=name asc")]
    [InlineData("grid", "grid-sort=grid-sum-asc")]
    public void Indexer_NotSorted(String name, String query)
    {
        sumColumn.Name = "sum";
        sumColumn.Grid.Name = name;
        sumColumn.Sort.IsEnabled = true;
        sumColumn.Grid.Query = HttpUtility.ParseQueryString(query);

        Assert.Null(sort[sumColumn]);
    }

    [Theory]
    [InlineData("", "sort=sum asc", 0)]
    [InlineData("", "SORT=SUM ASC", 0)]
    [InlineData("", "sort=sum asc,sum desc", 0)]
    [InlineData("", "SORT=SURNAME ASC,SUM ASC", 0)]
    [InlineData("", "sort=name asc,sum asc", 1)]
    [InlineData("", "SORT=NAME ASC,SUM ASC", 1)]
    [InlineData("", "SORT=NAME ASC,SURNAME ASC,SUM ASC", 1)]
    [InlineData("grid", "grid-sort=sum asc", 0)]
    [InlineData("grid", "GRID-SORT=SUM ASC", 0)]
    [InlineData("grid", "grid-sort=sum asc,sum desc", 0)]
    [InlineData("grid", "GRID-SORT=SURNAME ASC,SUM ASC", 0)]
    [InlineData("grid", "grid-sort=name asc,sum asc", 1)]
    [InlineData("grid", "GRID-SORT=NAME ASC,SUM ASC", 1)]
    [InlineData("grid", "GRID-SORT=SURNAME ASC,NAME ASC,SUM ASC", 1)]
    public void Indexer_SortIndex(String name, String query, Int32 index)
    {
        sumColumn.Name = "sum";
        sumColumn.Grid.Name = name;
        sumColumn.Sort.IsEnabled = true;
        sumColumn.Grid.Query = HttpUtility.ParseQueryString(query);

        Int32? expected = index;
        Int32? actual = sort[sumColumn]?.Index;

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", "sort=sum asc", GridSortOrder.Asc)]
    [InlineData("", "SORT=SUM ASC", GridSortOrder.Asc)]
    [InlineData("", "sort=sum desc", GridSortOrder.Desc)]
    [InlineData("", "SORT=SUM DESC", GridSortOrder.Desc)]
    [InlineData("", "SORT=SUM DESC,SUM ASC", GridSortOrder.Desc)]
    [InlineData("grid", "grid-sort=sum asc", GridSortOrder.Asc)]
    [InlineData("grid", "GRID-SORT=SUM ASC", GridSortOrder.Asc)]
    [InlineData("grid", "grid-sort=sum desc", GridSortOrder.Desc)]
    [InlineData("grid", "GRID-SORT=SUM DESC", GridSortOrder.Desc)]
    [InlineData("grid", "GRID-SORT=SUM DESC, SUM ASC", GridSortOrder.Desc)]
    public void Indexer_SortOrder(String name, String query, GridSortOrder order)
    {
        sumColumn.Name = "sum";
        sumColumn.Grid.Name = name;
        sumColumn.Sort.IsEnabled = true;
        sumColumn.Grid.Query = HttpUtility.ParseQueryString(query);

        GridSortOrder? actual = sort[sumColumn]?.Order;
        GridSortOrder? expected = order;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Indexer_SortAvailableAfterSettingQuery()
    {
        sumColumn.Name = "sum";
        sumColumn.Grid.Query = null;
        sumColumn.Sort.IsEnabled = true;

        Assert.Null(sort[sumColumn]);

        sumColumn.Grid.Query = HttpUtility.ParseQueryString("sort=sum asc");

        GridSortOrder? expected = GridSortOrder.Asc;
        GridSortOrder? actual = sort[sumColumn]?.Order;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Indexer_CachesSort()
    {
        sumColumn.Name = "sum";
        sumColumn.Sort.IsEnabled = true;
        sumColumn.Grid.Query = HttpUtility.ParseQueryString("sort=sum asc");

        (Int32 Index, GridSortOrder Order)? expected = sort[sumColumn];

        sumColumn.Grid.Query = HttpUtility.ParseQueryString("sort=name desc");

        (Int32 Index, GridSortOrder Order)? actual = sort[sumColumn];

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Process_SortsEnabledColumns()
    {
        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "a", Text = "a", Sum = 20 },
            new GridModel { Name = "b", Text = "d", Sum = 40 },
            new GridModel { Name = "b", Text = "e", Sum = 30 },
            new GridModel { Name = "b", Text = "b", Sum = 10 },
            new GridModel { Name = "a", Text = "c", Sum = 10 }
        }.AsQueryable();

        sumColumn.Sort.IsEnabled = true;
        nameColumn.Sort.IsEnabled = true;
        textColumn.Sort.IsEnabled = false;
        sort.Grid.Query = HttpUtility.ParseQueryString("sort=text asc,sum asc,name asc");

        IQueryable<GridModel> expected = items.OrderBy(item => item.Sum).ThenBy(item => item.Name);
        IQueryable<GridModel> actual = sort.Process(items);

        Assert.Equal(expected, actual);
    }
}
