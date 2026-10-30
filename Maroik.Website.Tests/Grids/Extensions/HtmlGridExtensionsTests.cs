using System.Globalization;
using Maroik.Website.Grids;
using Maroik.Website.Grids.Configurations;
using Maroik.Website.Grids.Extensions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Pagers;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;

namespace Maroik.Website.Tests.Grids.Extensions;

public class HtmlGridExtensionsTests
{
    private HtmlGrid<GridModel> htmlGrid;

    public HtmlGridExtensionsTests()
    {
        Grid<GridModel> grid = new(new GridModel[8]);
        IHtmlHelper html = Fake.Of<IHtmlHelper>();
        Mock.Get(html).Setup(mock => mock.ViewContext).Returns(new ViewContext { HttpContext = new DefaultHttpContext() });

        htmlGrid = new HtmlGrid<GridModel>(html, grid);
        grid.Columns.Add(model => model.Name);
        grid.Columns.Add(model => model.Sum);
    }

    [Fact]
    public void Build_Columns()
    {
        List<IGridColumnsOf<GridModel>> calls = [];

        htmlGrid.Build(calls.Add);

        Assert.Same(htmlGrid.Grid.Columns, Assert.Single(calls));
    }

    [Fact]
    public void Build_AddsSortProcessor()
    {
        htmlGrid.Grid.Processors.Clear();

        htmlGrid.Build(_ => { });

        Object expected = htmlGrid.Grid.Sort;
        Object actual = htmlGrid.Grid.Processors.Single();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Build_ReturnsHtmlGrid()
    {
        Object expected = htmlGrid;
        Object actual = htmlGrid.Build(_ => { });

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(null, GridFilterType.Double)]
    [InlineData(GridFilterType.Single, GridFilterType.Single)]
    [InlineData(GridFilterType.Double, GridFilterType.Double)]
    public void Filterable_SetsType(GridFilterType? current, GridFilterType type)
    {
        foreach (IGridColumn<GridModel> column in htmlGrid.Grid.Columns)
            column.Filter.Type = current;

        htmlGrid.Filterable(GridFilterType.Double);

        foreach (IGridColumn<GridModel> actual in htmlGrid.Grid.Columns)
            Assert.Equal(type, actual.Filter.Type);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Filterable_SetsIsEnabled(Boolean? current, Boolean isEnabled)
    {
        foreach (IGridColumn<GridModel> column in htmlGrid.Grid.Columns)
            column.Filter.IsEnabled = current;

        htmlGrid.Filterable();

        foreach (IGridColumn<GridModel> actual in htmlGrid.Grid.Columns)
            Assert.Equal(isEnabled, actual.Filter.IsEnabled);
    }

    [Fact]
    public void Filterable_ReturnsHtmlGrid()
    {
        Object expected = htmlGrid;
        Object actual = htmlGrid.Filterable();

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Filterable_Case_SetsIsEnabled(Boolean? current, Boolean isEnabled)
    {
        foreach (IGridColumn<GridModel> column in htmlGrid.Grid.Columns)
            column.Filter.IsEnabled = current;

        htmlGrid.Filterable(GridFilterCase.Original);

        foreach (IGridColumn<GridModel> actual in htmlGrid.Grid.Columns)
            Assert.Equal(isEnabled, actual.Filter.IsEnabled);
    }

    [Theory]
    [InlineData(null, GridFilterCase.Lower)]
    [InlineData(GridFilterCase.Lower, GridFilterCase.Lower)]
    [InlineData(GridFilterCase.Upper, GridFilterCase.Upper)]
    [InlineData(GridFilterCase.Original, GridFilterCase.Original)]
    public void Filterable_SetsCase(GridFilterCase? current, GridFilterCase filterCase)
    {
        foreach (IGridColumn<GridModel> column in htmlGrid.Grid.Columns)
            column.Filter.Case = current;

        htmlGrid.Filterable(GridFilterCase.Lower);

        foreach (IGridColumn<GridModel> actual in htmlGrid.Grid.Columns)
            Assert.Equal(filterCase, actual.Filter.Case);
    }

    [Fact]
    public void Filterable_Case_ReturnsHtmlGrid()
    {
        Object expected = htmlGrid;
        Object actual = htmlGrid.Filterable(GridFilterCase.Upper);

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Sortable_SetsIsEnabled(Boolean? current, Boolean? isEnabled)
    {
        foreach (IGridColumn<GridModel> column in htmlGrid.Grid.Columns)
            column.Sort.IsEnabled = current;

        htmlGrid.Sortable();

        foreach (IGridColumn<GridModel> actual in htmlGrid.Grid.Columns)
            Assert.Equal(isEnabled, actual.Sort.IsEnabled);
    }

    [Fact]
    public void Sortable_ReturnsHtmlGrid()
    {
        Assert.Equal(htmlGrid, htmlGrid.Sortable());
    }

    [Fact]
    public void RowAttributed_SetsRowAttributes()
    {
        Func<GridModel, Object> expected = (_) => new { data_id = 1 };
        Func<GridModel, Object>? actual = htmlGrid.RowAttributed(expected).Grid.Rows.Attributes;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void RowAttributed_ReturnsHtmlGrid()
    {
        Object expected = htmlGrid;
        Object actual = htmlGrid.RowAttributed(_ => new { });

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Attributed_SetsAttributes()
    {
        htmlGrid.Grid.Attributes["width"] = 10;
        htmlGrid.Grid.Attributes["class"] = "test";

        Dictionary<String, Object?> expected = new() { ["width"] = 1, ["class"] = "test" };
        IDictionary<String, Object?> actual = htmlGrid.Attributed(new { width = 1 }).Grid.Attributes;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Attributed_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Attributed(new { width = 1 }));
    }

    [Fact]
    public void AppendsCss_Class()
    {
        htmlGrid.Grid.Attributes.Clear();

        Assert.Equal("test", htmlGrid.AppendCss("test").Grid.Attributes["class"]);
    }

    [Theory]
    [InlineData("", "", "")]
    [InlineData("", " ", "")]
    [InlineData("", "test", "test")]
    [InlineData("", " test", "test")]
    [InlineData("", "test ", "test")]
    [InlineData("", " test ", "test")]

    [InlineData(" ", "", "")]
    [InlineData(" ", " ", "")]
    [InlineData(" ", "test", "test")]
    [InlineData(" ", " test", "test")]
    [InlineData(" ", "test ", "test")]
    [InlineData(" ", " test ", "test")]

    [InlineData(null, "", "")]
    [InlineData(null, " ", "")]
    [InlineData(null, "test", "test")]
    [InlineData(null, " test", "test")]
    [InlineData(null, "test ", "test")]
    [InlineData(null, " test ", "test")]

    [InlineData("first", "", "first")]
    [InlineData("first", "test", "first test")]
    [InlineData("first", " test", "first test")]
    [InlineData("first", "test ", "first test")]
    [InlineData("first", " test ", "first test")]
    [InlineData("first ", " test ", "first  test")]
    [InlineData(" first ", " test ", "first  test")]
    public void AppendsCss_Classes(String? current, String toAppend, String css)
    {
        htmlGrid.Grid.Attributes["class"] = current;

        Assert.Equal(css, htmlGrid.AppendCss(toAppend).Grid.Attributes["class"]);
    }

    [Fact]
    public void AppendsCss_ReturnsHtmlGrid()
    {
        Object expected = htmlGrid;
        Object actual = htmlGrid.AppendCss("column-class");

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Css_SetsCssClasses()
    {
        Assert.Equal("table", htmlGrid.Css(" table ").Grid.Attributes["class"]);
    }

    [Fact]
    public void Css_ReturnsHtmlGrid()
    {
        Assert.Equal(htmlGrid, htmlGrid.Css(""));
    }

    [Fact]
    public void Empty_SetsEmptyHtmlText()
    {
        Assert.Equal("<Text>", htmlGrid.Empty(new HtmlString("<Text>")).Grid.EmptyText);
    }

    [Fact]
    public void Empty_Html_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Empty(new HtmlString("Text")));
    }

    [Fact]
    public void Empty_SetsEncodedEmptyText()
    {
        Assert.Equal("&lt;Text&gt;", htmlGrid.Empty("<Text>").Grid.EmptyText);
    }

    [Fact]
    public void Empty_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Empty("Text"));
    }

    [Fact]
    public void Named_SetsName()
    {
        Assert.Equal("Test", htmlGrid.Named("Test").Grid.Name);
    }

    [Fact]
    public void Named_ReturnsHtmlGrid()
    {
        Object expected = htmlGrid;
        Object actual = htmlGrid.Named("Name");

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Id_SetsId()
    {
        Assert.Equal("Test", htmlGrid.Id("Test").Grid.Id);
    }

    [Fact]
    public void Id_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Id("Test"));
    }

    [Fact]
    public void UsingFooter_SetsFooterPartialViewName()
    {
        Assert.Equal("Partial", htmlGrid.UsingFooter("Partial").Grid.FooterPartialViewName);
    }

    [Fact]
    public void UsingFooter_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.UsingFooter("Partial"));
    }

    [Fact]
    public void Using_Processor_AddsProcessorToGrid()
    {
        IGridProcessor<GridModel> processor = Fake.Of<IGridProcessor<GridModel>>();
        htmlGrid.Grid.Processors.Clear();

        htmlGrid.Using(processor);

        Assert.Same(processor, htmlGrid.Grid.Processors.Single());
    }

    [Fact]
    public void Using_Processor_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Using(Fake.Of<IGridProcessor<GridModel>>()));
    }

    [Fact]
    public void Using_ProcessingMode_SetsProcessingMode()
    {
        GridProcessingMode actual = htmlGrid.Using(GridProcessingMode.Manual).Grid.Mode;
        GridProcessingMode expected = GridProcessingMode.Manual;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Using_ProcessingMode_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Using(GridProcessingMode.Manual));
    }

    [Fact]
    public void Using_FilterMode_SetsFilterMode()
    {
        GridFilterMode actual = htmlGrid.Using(GridFilterMode.Row).Grid.FilterMode;
        GridFilterMode expected = GridFilterMode.Row;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Using_FilterMode_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Using(GridFilterMode.Header));
    }

    [Fact]
    public void Using_Culture_SetsCulture()
    {
        CultureInfo actual = htmlGrid.Using(new CultureInfo("fr-FR")).Grid.Culture;
        CultureInfo expected = new("fr-FR");

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Using_Culture_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.Using(new CultureInfo("fr-FR")));
    }

    [Fact]
    public void UsingUrl_SetsUrl()
    {
        Assert.Same("/test/index", htmlGrid.UsingUrl("/test/index").Grid.Url);
    }

    [Fact]
    public void UsingUrl_ReturnsHtmlGrid()
    {
        Assert.Same(htmlGrid, htmlGrid.UsingUrl(""));
    }

    [Fact]
    public void Pageable_DoesNotChangePager()
    {
        GridPager<GridModel> pager = new(htmlGrid.Grid);
        htmlGrid.Grid.Pager = pager;

        htmlGrid.Pageable();

        Assert.Equal(pager, htmlGrid.Grid.Pager);
    }

    [Fact]
    public void Pageable_CreatesGridPager()
    {
        htmlGrid.Grid.Pager = null;

        htmlGrid.Pageable();

        GridPager<GridModel> expected = new(htmlGrid.Grid);
        IGridPager<GridModel> actual = htmlGrid.Grid.Pager!;

        Assert.Equal(expected.FirstDisplayPage, actual.FirstDisplayPage);
        Assert.Equal(expected.PartialViewName, actual.PartialViewName);
        Assert.Equal(expected.PagesToDisplay, actual.PagesToDisplay);
        Assert.Equal(expected.ProcessorType, actual.ProcessorType);
        Assert.Equal(expected.CurrentPage, actual.CurrentPage);
        Assert.Equal(expected.RowsPerPage, actual.RowsPerPage);
        Assert.Equal(expected.TotalPages, actual.TotalPages);
        Assert.Equal(expected.TotalRows, actual.TotalRows);
        Assert.Same(expected.Grid, actual.Grid);
    }

    [Fact]
    public void Pageable_ConfiguresPager()
    {
        htmlGrid.Grid.Pager = new GridPager<GridModel>(htmlGrid.Grid);
        IGridPager expected = htmlGrid.Grid.Pager;
        Boolean builderCalled = false;

        htmlGrid.Pageable(pager =>
        {
            Assert.Same(expected, pager);
            builderCalled = true;
        });

        Assert.True(builderCalled);
    }

    [Fact]
    public void Pageable_AddsGridProcessor()
    {
        htmlGrid.Grid.Processors.Clear();

        htmlGrid.Pageable();

        Object actual = htmlGrid.Grid.Processors.Single();
        Object expected = htmlGrid.Grid.Pager!;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Pageable_ReturnsHtmlGrid()
    {
        Assert.Equal(htmlGrid, htmlGrid.Pageable());
    }

    [Fact]
    public void Configure_ColumnOrder()
    {
        htmlGrid.Grid.Columns.Clear();
        IGridColumn<GridModel> empty = htmlGrid.Grid.Columns.Add(_ => "");
        IGridColumn<GridModel> sum = htmlGrid.Grid.Columns.Add(model => model.Sum);
        IGridColumn<GridModel> date = htmlGrid.Grid.Columns.Add(model => model.Date);
        IGridColumn<GridModel> name = htmlGrid.Grid.Columns.Add(model => model.Name);

        htmlGrid.Configure(new GridConfig
        {
            Name = "Test",
            Columns =
            [
                new GridColumnConfig { Name = date.Name },
                new GridColumnConfig { Name = sum.Name },
                new GridColumnConfig { Name = name.Name }
            ]
        });

        IList<IGridColumn<GridModel>> expected = [date, sum, name, empty];
        IList<IGridColumn<GridModel>> actual = htmlGrid.Grid.Columns;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Configure_ColumnWidth()
    {
        htmlGrid.Grid.Columns.Clear();
        IGridColumn<GridModel> empty = htmlGrid.Grid.Columns.Add(_ => "");
        IGridColumn<GridModel> sum = htmlGrid.Grid.Columns.Add(model => model.Sum);
        IGridColumn<GridModel> name = htmlGrid.Grid.Columns.Add(model => model.Name);
        IGridColumn<GridModel> date = htmlGrid.Grid.Columns.Add(model => model.Date).Hidden();

        htmlGrid.Configure(new GridConfig
        {
            Name = "Test",
            Columns =
            [
                new GridColumnConfig { Name = sum.Name, Width = "50%" },
                new GridColumnConfig { Name = date.Name, Width = "30px" },
                new GridColumnConfig { Name = name.Name, Width = "10%; color: red" }
            ]
        });

        Assert.Equal("width: 30px", date.Style);
        Assert.Equal("width: 10%", name.Style);
        Assert.Equal("width: 50%", sum.Style);
        Assert.Empty(empty.Style);
    }

    [Fact]
    public void Configure_ColumnVisibility()
    {
        htmlGrid.Grid.Columns.Clear();
        IGridColumn<GridModel> empty = htmlGrid.Grid.Columns.Add(_ => "");
        IGridColumn<GridModel> sum = htmlGrid.Grid.Columns.Add(model => model.Sum);
        IGridColumn<GridModel> name = htmlGrid.Grid.Columns.Add(model => model.Name);
        IGridColumn<GridModel> date = htmlGrid.Grid.Columns.Add(model => model.Date).Hidden();

        htmlGrid.Configure(new GridConfig
        {
            Name = "Test",
            Columns =
            [
                new GridColumnConfig { Name = sum.Name, Hidden = true },
                new GridColumnConfig { Name = date.Name, Hidden = true },
                new GridColumnConfig { Name = name.Name, Hidden = false }
            ]
        });

        Assert.False(empty.IsHidden);
        Assert.False(name.IsHidden);
        Assert.True(date.IsHidden);
        Assert.True(sum.IsHidden);
    }
}
