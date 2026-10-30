using System.Text.Encodings.Web;
using Maroik.Website.Grids;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;

namespace Maroik.Website.Tests.Grids;

public class HtmlGridTests
{
    private HtmlGrid<GridModel> htmlGrid;

    public HtmlGridTests()
    {
        Grid<GridModel> grid = new(new GridModel[8]);
        IHtmlHelper html = Fake.Of<IHtmlHelper>();
        Mock.Get(html).Setup(mock => mock.ViewContext).Returns(new ViewContext { HttpContext = new DefaultHttpContext() });

        htmlGrid = new HtmlGrid<GridModel>(html, grid);

        grid.Columns.Add(model => model.Name);
        grid.Columns.Add(model => model.Sum);
    }

    [Fact]
    public void HtmlGrid_DoesNotChangeQuery()
    {
        Object expected = htmlGrid.Grid.Query = new QueryCollection();
        Object? actual = new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).Grid.Query;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void HtmlGrid_SetsRequestGridQuery()
    {
        htmlGrid.Grid.Query = null;
        Mock.Get(htmlGrid.Html).Setup(mock => mock.ViewContext).Returns(new ViewContext());
        htmlGrid.Html.ViewContext.HttpContext = new DefaultHttpContext();

        Object expected = htmlGrid.Html.ViewContext.HttpContext.Request.Query;
        Object? actual = new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).Grid.Query;

        Assert.Same(expected, actual);
        Assert.NotNull(actual);
    }

    [Fact]
    public void HtmlGrid_SetsEmptyGridQuery()
    {
        htmlGrid.Grid.Query = null;
        Mock.Get(htmlGrid.Html).Setup(mock => mock.ViewContext).Returns((ViewContext)null!);

        Assert.Empty(new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).Grid.Query!);
    }

    [Fact]
    public void HtmlGrid_DoesNotChangeHttpContext()
    {
        htmlGrid.Grid.HttpContext = new DefaultHttpContext();
        Mock.Get(htmlGrid.Html).Setup(mock => mock.ViewContext).Returns(new ViewContext { HttpContext = new DefaultHttpContext() });

        Object? expected = htmlGrid.Grid.HttpContext;
        Object? actual = new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).Grid.HttpContext;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void HtmlGrid_SetsHttpContext()
    {
        htmlGrid.Grid.HttpContext = null;
        Mock.Get(htmlGrid.Html).Setup(mock => mock.ViewContext).Returns(new ViewContext());
        htmlGrid.Html.ViewContext.HttpContext = new DefaultHttpContext();

        Object? actual = new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).Grid.HttpContext;
        Object? expected = htmlGrid.Html.ViewContext.HttpContext;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void HtmlGrid_SetsPartialViewName()
    {
        Assert.Equal("MvcGrid/_Grid", new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).PartialViewName);
    }

    [Fact]
    public void HtmlGrid_SetsHtml()
    {
        Object actual = new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).Html;
        Object expected = htmlGrid.Html;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void HtmlGrid_SetsGrid()
    {
        Object actual = new HtmlGrid<GridModel>(htmlGrid.Html, htmlGrid.Grid).Grid;
        Object expected = htmlGrid.Grid;

        Assert.Same(expected, actual);
    }

    [Fact]
    public async Task RenderAsync_RendersThePartialView()
    {
        await using StringWriter writer = new();
        Task<IHtmlContent> view = Task.FromResult<IHtmlContent>(new HtmlString("Test"));
        Mock.Get(htmlGrid.Html).Setup(mock => mock.PartialAsync(htmlGrid.PartialViewName, htmlGrid.Grid, null)).Returns(view);

        (await htmlGrid.RenderAsync()).WriteTo(writer, HtmlEncoder.Default);

        Assert.Equal("Test", writer.ToString());
    }
}
