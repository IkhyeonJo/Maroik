using System.Collections;
using Maroik.Website.Grids;
using Maroik.Website.Grids.Pagers;
using Maroik.Website.Grids.Processors;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.AspNetCore.Http;

namespace Maroik.Website.Tests.Grids.Pagers;

public class GridPagerTests
{
    private GridPager<GridModel> pager;

    public GridPagerTests()
    {
        pager = new GridPager<GridModel>(new Grid<GridModel>([]) { Query = new QueryCollection() });
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 1, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(1, 20, 1)]
    [InlineData(19, 20, 1)]
    [InlineData(20, 20, 1)]
    [InlineData(21, 20, 2)]
    [InlineData(39, 20, 2)]
    [InlineData(40, 20, 2)]
    [InlineData(41, 20, 3)]
    public void TotalPages_ReturnsTotalPages(Int32 total, Int32 rows, Int32 pages)
    {
        pager.RowsPerPage = rows;
        pager.TotalRows = total;

        Assert.Equal(pager.TotalPages, pages);
    }

    [Fact]
    public void CurrentPage_NullQuery()
    {
        pager.TotalRows = 100;
        pager.CurrentPage = 3;
        pager.Grid.Query = null;

        Assert.Equal(3, pager.CurrentPage);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("", "grid-page=")]
    [InlineData("", "grid-page=2a")]
    [InlineData("grid", "")]
    [InlineData("grid", "grid-page=")]
    [InlineData("grid", "grid-page=2a")]
    public void CurrentPage_OnInvalidQueryPageUsesCurrentPage(String name, String query)
    {
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.Grid.Name = name;
        pager.CurrentPage = 3;
        pager.TotalRows = 100;

        Assert.Equal(3, pager.CurrentPage);
    }

    [Theory]
    [InlineData("", "page=5")]
    [InlineData("grid", "grid-page=5")]
    public void CurrentPage_OnGreaterThanTotalPagesReturnsTotalPages(String name, String query)
    {
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.RowsPerPage = 25;
        pager.Grid.Name = name;
        pager.TotalRows = 100;

        Assert.Equal(4, pager.CurrentPage);
    }

    [Theory]
    [InlineData("", "page=0")]
    [InlineData("", "page=-1")]
    [InlineData("grid", "grid-page=0")]
    [InlineData("grid", "grid-page=-1")]
    public void CurrentPage_OnLessOrEqualToZeroQueryPageReturnsOne(String name, String query)
    {
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.Grid.Name = name;
        pager.CurrentPage = 5;

        Assert.Equal(1, pager.CurrentPage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CurrentPage_OnLessOrEqualToZeroCurrentPageReturnsOne(Int32 page)
    {
        pager.CurrentPage = page;

        Assert.Equal(1, pager.CurrentPage);
    }

    [Theory]
    [InlineData("", "page=2")]
    [InlineData("", "PAGE=2")]
    [InlineData("grid", "grid-page=2")]
    [InlineData("grid", "GRID-PAGE=2")]
    public void CurrentPage_SetsCurrentPageFromQuery(String name, String query)
    {
        pager.Grid.Name = name;
        pager.Grid.Query = HttpUtility.ParseQueryString(query);

        pager.TotalRows = 4 * pager.RowsPerPage;

        Assert.Equal(2, pager.CurrentPage);
    }

    [Fact]
    public void RowsPerPage_NullQuery()
    {
        pager.RowsPerPage = 3;
        pager.Grid.Query = null;
        pager.PageSizes.Clear();
        pager.ShowPageSizes = true;

        Assert.Equal(3, pager.RowsPerPage);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("grid", "")]
    [InlineData("", "rows=")]
    [InlineData("", "rows=2a")]
    [InlineData("grid", "grid-rows=")]
    [InlineData("grid", "grid-rows=2a")]
    public void RowsPerPage_OnInvalidQueryRowsUsesRowsPerPage(String name, String query)
    {
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.ShowPageSizes = true;
        pager.PageSizes.Clear();
        pager.Grid.Name = name;
        pager.RowsPerPage = 33;
        pager.TotalRows = 500;

        Assert.Equal(33, pager.RowsPerPage);
    }

    [Theory]
    [InlineData("", "rows=-1")]
    [InlineData("", "rows=-10")]
    [InlineData("grid", "grid-rows=-1")]
    [InlineData("grid", "grid-rows=-10")]
    public void RowsPerPage_MinimumQueryPage(String name, String query)
    {
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.ShowPageSizes = true;
        pager.PageSizes.Clear();
        pager.Grid.Name = name;
        pager.RowsPerPage = 5;

        Assert.Equal(1, pager.RowsPerPage);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-10)]
    public void RowsPerPage_MinimumPage(Int32 rows)
    {
        pager.PageSizes.Clear();
        pager.RowsPerPage = rows;
        pager.ShowPageSizes = true;

        Assert.Equal(1, pager.RowsPerPage);
    }

    [Theory]
    [InlineData("", "rows=-1", 10)]
    [InlineData("", "rows=0", 10)]
    [InlineData("", "rows=1", 10)]
    [InlineData("", "rows=9", 10)]
    [InlineData("", "rows=10", 10)]
    [InlineData("", "rows=11", 20)]
    [InlineData("", "rows=19", 20)]
    [InlineData("", "rows=20", 20)]
    [InlineData("", "rows=21", 20)]
    [InlineData("grid", "grid-rows=-1", 10)]
    [InlineData("grid", "grid-rows=0", 10)]
    [InlineData("grid", "grid-rows=1", 10)]
    [InlineData("grid", "grid-rows=9", 10)]
    [InlineData("grid", "grid-rows=10", 10)]
    [InlineData("grid", "grid-rows=11", 20)]
    [InlineData("grid", "grid-rows=19", 20)]
    [InlineData("grid", "grid-rows=20", 20)]
    [InlineData("grid", "grid-rows=21", 20)]
    public void RowsPerPage_AllowsOnlyFromPageSizes(String name, String query, Int32 rows)
    {
        pager.PageSizes = new Dictionary<Int32, String> { [10] = "10", [20] = "20" };
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.ShowPageSizes = true;
        pager.Grid.Name = name;

        Assert.Equal(rows, pager.RowsPerPage);
    }

    [Theory]
    [InlineData("", "rows=-1", 10)]
    [InlineData("", "rows=0", 0)]
    [InlineData("", "rows=1", 10)]
    [InlineData("", "rows=9", 10)]
    [InlineData("", "rows=10", 10)]
    [InlineData("", "rows=11", 20)]
    [InlineData("", "rows=19", 20)]
    [InlineData("", "rows=20", 20)]
    [InlineData("", "rows=21", 0)]
    [InlineData("grid", "grid-rows=-1", 10)]
    [InlineData("grid", "grid-rows=0", 0)]
    [InlineData("grid", "grid-rows=1", 10)]
    [InlineData("grid", "grid-rows=9", 10)]
    [InlineData("grid", "grid-rows=10", 10)]
    [InlineData("grid", "grid-rows=11", 20)]
    [InlineData("grid", "grid-rows=19", 20)]
    [InlineData("grid", "grid-rows=20", 20)]
    [InlineData("grid", "grid-rows=21", 0)]
    public void RowsPerPage_Unlimited_AllowsOnlyFromPageSizes(String name, String query, Int32 rows)
    {
        pager.PageSizes = new Dictionary<Int32, String> { [0] = "All", [10] = "10", [20] = "20" };
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.ShowPageSizes = true;
        pager.Grid.Name = name;

        Assert.Equal(rows, pager.RowsPerPage);
    }

    [Theory]
    [InlineData("", "rows=123")]
    [InlineData("", "ROWS=123")]
    [InlineData("grid", "grid-rows=123")]
    [InlineData("grid", "GRID-ROWS=123")]
    public void RowsPerPage_SetsRowsPerPageFromQuery(String name, String query)
    {
        pager.Grid.Query = HttpUtility.ParseQueryString(query);
        pager.ShowPageSizes = true;
        pager.PageSizes.Clear();
        pager.Grid.Name = name;

        Assert.Equal(123, pager.RowsPerPage);
    }

    [Fact]
    public void RowsPerPage_DoesNotUseQuery()
    {
        pager.Grid.Query = HttpUtility.ParseQueryString("rows=2");
        pager.ShowPageSizes = false;
        pager.RowsPerPage = 1;

        Assert.Equal(1, pager.RowsPerPage);
    }

    [Theory]
    [InlineData(1, 1, 1)]
    [InlineData(1, 2, 2)]
    [InlineData(1, 3, 3)]
    [InlineData(1, 4, 4)]
    [InlineData(1, 5, 5)]
    [InlineData(2, 1, 1)]
    [InlineData(2, 2, 2)]
    [InlineData(2, 3, 3)]
    [InlineData(2, 4, 4)]
    [InlineData(2, 5, 4)]
    [InlineData(3, 1, 1)]
    [InlineData(3, 2, 1)]
    [InlineData(3, 3, 2)]
    [InlineData(3, 4, 3)]
    [InlineData(3, 5, 3)]
    [InlineData(4, 1, 1)]
    [InlineData(4, 2, 1)]
    [InlineData(4, 3, 2)]
    [InlineData(4, 4, 2)]
    [InlineData(4, 5, 2)]
    [InlineData(5, 1, 1)]
    [InlineData(5, 2, 1)]
    [InlineData(5, 3, 1)]
    [InlineData(5, 4, 1)]
    [InlineData(5, 5, 1)]
    [InlineData(6, 1, 1)]
    [InlineData(6, 2, 1)]
    [InlineData(6, 3, 1)]
    [InlineData(6, 4, 1)]
    [InlineData(6, 5, 1)]
    public void FirstDisplayPage_ReturnsFirstDisplayPage(Int32 display, Int32 page, Int32 first)
    {
        pager.Grid.Query = HttpUtility.ParseQueryString("page=" + page);
        pager.PagesToDisplay = display;
        pager.RowsPerPage = 1;
        pager.TotalRows = 5;

        Assert.Equal(first, pager.FirstDisplayPage);
    }

    [Fact]
    public void GridPager_SetsGrid()
    {
        Object actual = new GridPager<GridModel>(pager.Grid).Grid;
        Object expected = pager.Grid;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GridPager_SetsCssClasses()
    {
        Assert.Empty(new GridPager<GridModel>(pager.Grid).CssClasses);
    }

    [Fact]
    public void GridPager_SetsCurrentPage()
    {
        Assert.Equal(1, new GridPager<GridModel>(pager.Grid).CurrentPage);
    }

    [Fact]
    public void GridPager_SetsRowsPerPage()
    {
        Assert.Equal(20, new GridPager<GridModel>(pager.Grid).RowsPerPage);
    }

    [Fact]
    public void GridPager_SetsPagesToDisplay()
    {
        Assert.Equal(5, new GridPager<GridModel>(pager.Grid).PagesToDisplay);
    }

    [Fact]
    public void GridPager_SetsDefaultPartialViewName()
    {
        Assert.Equal("MvcGrid/_Pager", new GridPager<GridModel>(pager.Grid).PartialViewName);
    }

    [Fact]
    public void GridPager_SetsProcessorType()
    {
        GridProcessorType actual = new GridPager<GridModel>(pager.Grid).ProcessorType;
        GridProcessorType expected = GridProcessorType.Post;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GridPager_SetsDefaultPageSizes()
    {
        Dictionary<Int32, String> expected = new() { [10] = "10", [20] = "20", [50] = "50", [100] = "100" };
        Dictionary<Int32, String> actual = new GridPager<GridModel>(pager.Grid).PageSizes;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Process_SetsTotalRows()
    {
        pager.Process(new GridModel[100].AsQueryable());

        Assert.Equal(100, pager.TotalRows);
    }

    [Fact]
    public void Process_ReturnsAllItems()
    {
        IQueryable<GridModel> items = new[] { new GridModel(), new GridModel(), new GridModel() }.AsQueryable();
        pager.RowsPerPage = 0;
        pager.CurrentPage = 2;

        Assert.Equal(items.ToList(), pager.Process(items));
    }

    [Fact]
    public void Process_ReturnsPagedItems()
    {
        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "test", Sum = 5 },
            new GridModel { Name = "test", Sum = 2 },
            new GridModel { Name = "another", Sum = 3 },
            new GridModel { Name = "testing", Sum = 5 },
            new GridModel { Name = "nothing", Sum = 2 },
            new GridModel { Name = "nothing", Sum = 9 }
        }.AsQueryable().OrderBy(model => model.Name).ThenBy(model => model.Sum);
        pager.RowsPerPage = 2;
        pager.CurrentPage = 2;

        IEnumerable expected = items.Skip(2).Take(2);
        IEnumerable actual = pager.Process(items);

        Assert.Equal(expected, actual);
    }
}
