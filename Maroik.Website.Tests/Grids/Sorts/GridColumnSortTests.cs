using Maroik.Website.Grids;
using Maroik.Website.Grids.Columns;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Sorts;
using Maroik.Website.Tests.Grids.Fixtures;

namespace Maroik.Website.Tests.Grids.Sorts;

public class GridColumnSortTests
{
    private GridColumnSort<GridModel, Object?> sort;

    public GridColumnSortTests()
    {
        Grid<GridModel> grid = new([]);
        GridColumn<GridModel, Object?> column = new(grid, model => model.Name);

        sort = new GridColumnSort<GridModel, Object?>(column) { IsEnabled = true };
    }

    [Fact]
    public void Index_NotSorted_Null()
    {
        sort.Column.Grid.Sort = new FakeGridSort<GridModel> { OnItem = column => column == sort.Column ? null : null };

        Assert.Null(sort.Index);
    }

    [Fact]
    public void Index_ReturnsFromGridSort()
    {
        sort.Column.Grid.Sort = new FakeGridSort<GridModel> { OnItem = column => column == sort.Column ? (2, GridSortOrder.Desc) : null };

        Assert.Equal(2, sort.Index);
    }

    [Fact]
    public void Order_ReturnsFromGridSort()
    {
        sort.Column.Grid.Sort = new FakeGridSort<GridModel> { OnItem = column => column == sort.Column ? (0, GridSortOrder.Desc) : null };

        Assert.Equal(GridSortOrder.Desc, sort.Order);
    }

    [Fact]
    public void GridColumnSort_SetsColumn()
    {
        GridColumn<GridModel, String?> expected = new(sort.Column.Grid, model => model.Name);
        IGridColumn<GridModel, String?> actual = new GridColumnSort<GridModel, String?>(expected).Column;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GridColumnSort_NotMemberExpression_IsNotEnabled()
    {
        GridColumn<GridModel, String?> column = new(sort.Column.Grid, model => model.ToString());

        Assert.False(new GridColumnSort<GridModel, String?>(column).IsEnabled);
    }

    [Fact]
    public void GridColumnSort_MemberExpression_IsEnabledNull()
    {
        GridColumn<GridModel, String?> column = new(sort.Column.Grid, model => model.Name);

        Assert.Null(new GridColumnSort<GridModel, String?>(column).IsEnabled);
    }

    [Fact]
    public void GridColumnSort_SetsFirstOrder()
    {
        GridSortOrder actual = new GridColumnSort<GridModel, Object?>(sort.Column).FirstOrder;
        GridSortOrder expected = GridSortOrder.Asc;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void By_ReturnsSameItems()
    {
        IQueryable<GridModel> items = new GridModel[2].AsQueryable();
        sort.IsEnabled = false;

        Assert.Equal(items, sort.By(items));
    }

    [Fact]
    public void By_AscendingOrder()
    {
        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "b" },
            new GridModel { Name = "a" }
        }.AsQueryable();

        sort.Column.Grid.Sort = new FakeGridSort<GridModel> { OnItem = column => column == sort.Column ? (0, GridSortOrder.Asc) : null };

        IQueryable<GridModel> expected = items.OrderBy(sort.Column.Expression);
        IQueryable<GridModel> actual = sort.By(items);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void By_DescendingOrder()
    {
        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "a" },
            new GridModel { Name = "b" }
        }.AsQueryable();

        sort.Column.Grid.Sort = new FakeGridSort<GridModel> { OnItem = column => column == sort.Column ? (0, GridSortOrder.Desc) : null };

        IQueryable<GridModel> expected = items.OrderByDescending(sort.Column.Expression);
        IQueryable<GridModel> actual = sort.By(items);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ThenBy_ReturnsSameItems()
    {
        IOrderedQueryable<GridModel> items = new GridModel[2].AsQueryable().OrderBy(item => item.ShortText);
        sort.IsEnabled = false;

        Assert.Same(items, sort.ThenBy(items));
    }

    [Fact]
    public void ThenBy_AscendingOrder()
    {
        IOrderedQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "b", ShortText = "c" },
            new GridModel { Name = "a", ShortText = "c" }
        }.AsQueryable().OrderBy(item => item.ShortText);

        sort.Column.Grid.Sort = new FakeGridSort<GridModel> { OnItem = column => column == sort.Column ? (1, GridSortOrder.Asc) : null };

        IQueryable<GridModel> expected = items.ThenBy(item => item.Name);
        IQueryable<GridModel> actual = sort.ThenBy(items);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void ThenBy_DescendingOrder()
    {
        IOrderedQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "a", ShortText = "c" },
            new GridModel { Name = "b", ShortText = "c" }
        }.AsQueryable().OrderBy(item => item.ShortText);

        sort.Column.Grid.Sort = new FakeGridSort<GridModel> { OnItem = column => column == sort.Column ? (1, GridSortOrder.Desc) : null };

        IQueryable<GridModel> expected = items.ThenByDescending(item => item.Name);
        IQueryable<GridModel> actual = sort.ThenBy(items);

        Assert.Equal(expected, actual);
    }
}
