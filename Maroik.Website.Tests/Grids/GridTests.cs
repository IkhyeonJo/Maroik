using System.Globalization;
using Maroik.Website.Grids;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Tests.Grids.Fixtures;

namespace Maroik.Website.Tests.Grids;

public class GridTests
{
    [Fact]
    public void IGrid_ReturnsColumns()
    {
        Grid<GridModel> grid = new([]);

        Object actual = ((IGrid)grid).Columns;
        Object expected = grid.Columns;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void IGrid_ReturnsRows()
    {
        Grid<GridModel> grid = new([]);

        Assert.Equal(grid.Rows, ((IGrid)grid).Rows);
    }

    [Fact]
    public void IGrid_ReturnsPager()
    {
        Grid<GridModel> grid = new([]);

        Assert.Equal(grid.Pager, ((IGrid)grid).Pager);
    }

    [Fact]
    public void Grid_SetsName()
    {
        Assert.Empty(new Grid<GridModel>([]).Name);
    }

    [Fact]
    public void Grid_SetsUrl()
    {
        Assert.Empty(new Grid<GridModel>([]).Url);
    }

    [Fact]
    public void IGrid_SetsCulture()
    {
        Grid<GridModel> grid = new([], new CultureInfo("fr-FR"));

        Assert.Equal(new CultureInfo("fr-FR"), grid.Culture);
    }

    [Fact]
    public void IGrid_SetsDefaultCulture()
    {
        Grid<GridModel> grid = new([]);

        Assert.Equal(CultureInfo.CurrentCulture, grid.Culture);
    }

    [Fact]
    public void Grid_SetsFooterPartialViewName()
    {
        Assert.Empty(new Grid<GridModel>([]).FooterPartialViewName);
    }

    [Fact]
    public void Grid_SetsProcessors()
    {
        Assert.Empty(new Grid<GridModel>([]).Processors);
    }

    [Fact]
    public void Grid_SetsSource()
    {
        IQueryable<GridModel> expected = new GridModel[2].AsQueryable();
        IQueryable<GridModel> actual = new Grid<GridModel>(expected).Source;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Grid_SetsFilterMode()
    {
        GridFilterMode actual = new Grid<GridModel>([]).FilterMode;
        GridFilterMode expected = GridFilterMode.Excel;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Grid_SetsEmptyAttributes()
    {
        Assert.Empty(new Grid<GridModel>([]).Attributes);
    }

    [Fact]
    public void Grid_SetsEmptyColumns()
    {
        Assert.Empty(new Grid<GridModel>([]).Columns);
    }

    [Fact]
    public void Grid_SetsMode()
    {
        Assert.Equal(GridProcessingMode.Automatic, new Grid<GridModel>([]).Mode);
    }

    [Fact]
    public void Grid_SetsEmptyRows()
    {
        Assert.Empty(new Grid<GridModel>([]).Rows);
    }

    [Fact]
    public void Grid_SetsSort()
    {
        Assert.NotNull(new Grid<GridModel>([]).Sort);
    }
}
