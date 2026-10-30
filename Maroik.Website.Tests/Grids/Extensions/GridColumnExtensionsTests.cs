using Maroik.Website.Grids;
using Maroik.Website.Grids.Extensions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Sorts;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Maroik.Website.Tests.Grids.Extensions;

public class GridColumnExtensionsTests
{
    private IGridColumn<GridModel, String?> column;

    public GridColumnExtensionsTests()
    {
        column = new Grid<GridModel>(Array.Empty<GridModel>().AsQueryable()).Columns.Add(model => model.Name);
    }

    [Fact]
    public void RenderedAs_SetsIndexedRenderValue()
    {
        Func<GridModel, Int32, Object?> expected = (model, _) => model.Name;
        Func<GridModel, Int32, Object?>? actual = column.RenderedAs(expected).RenderValue;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void RenderedAs_ReturnsIndexedColumn()
    {
        Object expected = column;
        Object actual = column.RenderedAs((model, _) => model.Name);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void RenderedAs_SetsRenderValue()
    {
        GridModel gridModel = new() { Name = "Test" };

        Object? actual = column.RenderedAs(model => model.Name).RenderValue?.Invoke(gridModel, 0);
        Object? expected = gridModel.Name;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void RenderedAs_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.RenderedAs(model => model.Name);

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData("", "equals")]
    [InlineData("contains", "contains")]
    public void UsingFilterOptions_SetsDefaultFilterMethod(String current, String method)
    {
        column.Filter.DefaultMethod = current;

        Assert.Equal(method, column.UsingFilterOptions(Array.Empty<SelectListItem>()).Filter.DefaultMethod);
    }

    [Fact]
    public void UsingFilterOptions_Values()
    {
        IEnumerable<SelectListItem> expected = Array.Empty<SelectListItem>();
        IEnumerable<SelectListItem> actual = column.UsingFilterOptions(expected).Filter.Options;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void UsingFilterOptions_SourceValues()
    {
        column.Grid.Source = new[]
        {
            new GridModel { Name = "Test" },
            new GridModel { Name = "Next" },
            new GridModel { Name = "Next" },
            new GridModel { Name = "Last" }
        }.AsQueryable();

        using IEnumerator<SelectListItem> actual = column.UsingFilterOptions().Filter.Options.GetEnumerator();
        using IEnumerator<SelectListItem> expected = new List<SelectListItem>
        {
            new() { Value = null, Text = null },
            new() { Value = "Last", Text = "Last" },
            new() { Value = "Next", Text = "Next" },
            new() { Value = "Test", Text = "Test" }
        }.GetEnumerator();

        while (expected.MoveNext() | actual.MoveNext())
        {
            Assert.Same(expected.Current.Value, actual.Current.Value);
            Assert.Same(expected.Current.Text, actual.Current.Text);
        }
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void UsingFilterOptions_SetsIsEnabled(Boolean? current, Boolean enabled)
    {
        column.Filter.IsEnabled = current;

        Boolean? actual = column.UsingFilterOptions(Array.Empty<SelectListItem>()).Filter.IsEnabled;
        Boolean? expected = enabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UsingFilterOptions_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.UsingFilterOptions(Array.Empty<SelectListItem>());

        Assert.Same(expected, actual);
    }

    [Fact]
    public void UsingDefaultFilterMethod_SetsMethod()
    {
        Assert.Same("test", column.UsingDefaultFilterMethod("test").Filter.DefaultMethod);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void UsingDefaultFilterMethod_SetsIsEnabled(Boolean? current, Boolean enabled)
    {
        column.Filter.IsEnabled = current;

        Boolean? actual = column.UsingDefaultFilterMethod("test").Filter.IsEnabled;
        Boolean? expected = enabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void UsingDefaultFilterMethod_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.UsingDefaultFilterMethod("test");

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Filterable_Configure_SetsIsEnabled(Boolean? current, Boolean enabled)
    {
        column.Filter.IsEnabled = current;

        Boolean? actual = column.Filterable(_ => { }).Filter.IsEnabled;
        Boolean? expected = enabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filterable_ConfiguresColumn()
    {
        column.Filter.IsEnabled = null;

        column.Filterable(filter => filter.IsEnabled = false);

        Assert.False(column.Filter.IsEnabled);
    }

    [Fact]
    public void Filterable_Configure_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.Filterable(_ => { });

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Filterable_Case_SetsIsEnabled(Boolean? current, Boolean enabled)
    {
        column.Filter.IsEnabled = current;

        Boolean? actual = column.Filterable(GridFilterCase.Lower).Filter.IsEnabled;
        Boolean? expected = enabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filterable_SetsCase()
    {
        GridFilterCase? actual = column.Filterable(GridFilterCase.Lower).Filter.Case;
        GridFilterCase? expected = GridFilterCase.Lower;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filterable_Case_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.Filterable(GridFilterCase.Lower);

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Filterable_SetsIsEnabled(Boolean isEnabled)
    {
        Boolean? actual = column.Filterable(isEnabled).Filter.IsEnabled;
        Boolean? expected = isEnabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filterable_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.Filterable(true);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Filterable_SetsType()
    {
        GridFilterType? actual = column.Filterable(GridFilterType.Double).Filter.Type;
        GridFilterType? expected = GridFilterType.Double;

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Filterable_Type_SetsIsEnabled(Boolean? current, Boolean enabled)
    {
        column.Filter.IsEnabled = current;

        Boolean? actual = column.Filterable(GridFilterType.Single).Filter.IsEnabled;
        Boolean? expected = enabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filterable_Type_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.Filterable(GridFilterType.Single);

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Filterable_Name_SetsIsEnabled(Boolean? current, Boolean enabled)
    {
        column.Filter.IsEnabled = current;

        Boolean? actual = column.Filterable("test").Filter.IsEnabled;
        Boolean? expected = enabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Filterable_SetsName()
    {
        Assert.Equal("Numeric", column.Filterable("Numeric").Filter.Name);
    }

    [Fact]
    public void Filterable_Name_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.Filterable("Numeric");

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Sortable_SetsFirstOrder()
    {
        GridSortOrder? actual = column.Sortable(GridSortOrder.Desc).Sort.FirstOrder;
        GridSortOrder? expected = GridSortOrder.Desc;

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Sortable_FirstOrder_SetsIsEnabled(Boolean? current, Boolean enabled)
    {
        column.Sort.IsEnabled = current;

        Boolean? actual = column.Sortable(GridSortOrder.Desc).Sort.IsEnabled;
        Boolean? expected = enabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Sortable_FirstOrder_ReturnsColumn()
    {
        Object actual = column.Sortable(GridSortOrder.Desc);
        Object expected = column;

        Assert.Same(expected, actual);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Sortable_SetsIsEnabled(Boolean isEnabled)
    {
        Boolean? actual = column.Sortable(isEnabled).Sort.IsEnabled;
        Boolean? expected = isEnabled;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Sortable_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.Sortable(true);

        Assert.Same(expected, actual);
    }

    [Fact]
    public void Encoded_SetsIsEncoded()
    {
        Assert.True(column.Encoded(true).IsEncoded);
    }

    [Fact]
    public void Encoded_ReturnsColumn()
    {
        Assert.Equal(column, column.Encoded(true));
    }

    [Fact]
    public void Formatted_SetsFormat()
    {
        Assert.Equal("Format", column.Formatted("Format").Format);
    }

    [Fact]
    public void Formatted_ReturnsColumn()
    {
        Object expected = column;
        Object actual = column.Formatted("Format");

        Assert.Same(expected, actual);
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

    [InlineData("first", "", "first")]
    [InlineData("first", "test", "first test")]
    [InlineData("first", " test", "first test")]
    [InlineData("first", "test ", "first test")]
    [InlineData("first", " test ", "first test")]
    [InlineData("first ", " test ", "first  test")]
    [InlineData(" first ", " test ", "first  test")]
    public void AppendsCss_Classes(String current, String toAppend, String css)
    {
        column.CssClasses = current;

        Assert.Equal(css, column.AppendCss(toAppend).CssClasses);
    }

    [Fact]
    public void AppendsCss_ReturnsColumn()
    {
        Assert.Same(column, column.AppendCss("column-class"));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData("", "")]
    [InlineData("Title", "Title")]
    public void Titled_SetsTitle(Object rawTitle, Object title)
    {
        Assert.Equal(title, column.Titled(rawTitle).Title);
    }

    [Fact]
    public void Titled_ReturnsColumn()
    {
        Assert.Same(column, column.Titled("Title"));
    }

    [Fact]
    public void Width_SetsInvariantStyle()
    {
        Assert.Equal("width: -50px", column.Width(-50).Style);
    }

    [Fact]
    public void Width_ReturnsColumn()
    {
        Assert.Same(column, column.Width(10));
    }

    [Theory]
    [InlineData("100%", "width: 100%")]
    [InlineData("100em; color: blue", "width: 100em")]
    public void Width_SetsStyle(String width, String style)
    {
        Assert.Equal(style, column.Width(width).Style);
    }

    [Fact]
    public void Width_Style_ReturnsColumn()
    {
        Assert.Same(column, column.Width("50%"));
    }

    [Fact]
    public void Named_SetsName()
    {
        Assert.Equal("Test", column.Named("Test").Name);
    }

    [Fact]
    public void Named_ReturnsColumn()
    {
        Assert.Same(column, column.Named("Name"));
    }

    [Fact]
    public void Css_SetsCssClasses()
    {
        Assert.Equal("column-class", column.Css(" column-class ").CssClasses);
    }

    [Fact]
    public void Css_ReturnsColumn()
    {
        Assert.Equal(column, column.Css(""));
    }

    [Fact]
    public void Hidden_SetsHidden()
    {
        column.IsHidden = false;

        Assert.True(column.Hidden().IsHidden);
    }

    [Fact]
    public void Hidden_ReturnsColumn()
    {
        Assert.Equal(column, column.Hidden());
    }

    [Fact]
    public void AsAttributes_OnEmptyColumn()
    {
        column.Name = "";
        column.CssClasses = "";
        column.IsHidden = false;
        column.Filter.Name = "text";
        column.Sort.IsEnabled = false;
        column.Filter.IsEnabled = false;
        column.Filter.DefaultMethod = "equals";
        column.Filter.Type = GridFilterType.Double;
        column.Sort.FirstOrder = GridSortOrder.Desc;
        column.Filter.First = Fake.Of<IGridFilter>();

        Assert.Empty(column.AsAttributes());
    }

    [Fact]
    public void AsAttributes_OnPartialColumn()
    {
        column.Name = "";
        column.CssClasses = "";
        column.IsHidden = false;
        column.Filter.Name = "";
        column.Filter.Type = null;
        column.Filter.First = null;
        column.Filter.Second = null;
        column.Sort.IsEnabled = true;
        column.Filter.IsEnabled = true;
        column.Filter.DefaultMethod = "";
        column.Grid.Query = new QueryCollection();
        column.Sort.FirstOrder = GridSortOrder.Asc;

        IDictionary<String, Object?> actual = column.AsAttributes();

        Assert.Single(actual);
        Assert.Equal("filterable sortable", actual["class"]);
    }

    [Fact]
    public void AsAttributes_OnFullColumn()
    {
        column.Name = "name";
        column.IsHidden = true;
        column.Filter.Name = "text";
        column.Style = "width: 10px";
        column.Sort.IsEnabled = true;
        column.Filter.IsEnabled = true;
        column.CssClasses = "test-classes";
        column.Filter.DefaultMethod = "equals";
        column.Filter.Type = GridFilterType.Double;
        column.Sort.FirstOrder = GridSortOrder.Desc;
        column.Filter.First = Fake.Of<IGridFilter>();
        column.Grid.Query = HttpUtility.ParseQueryString("sort=name asc");

        IDictionary<String, Object?> actual = column.AsAttributes();

        Assert.Equal("test-classes filterable sortable asc mvc-grid-hidden", actual["class"]);
        Assert.Equal(GridFilterType.Double, actual["data-filter-type"]);
        Assert.Equal("equals", actual["data-filter-default-method"]);
        Assert.Equal(GridSortOrder.Desc, actual["data-sort-first"]);
        Assert.Equal(GridSortOrder.Asc, actual["data-sort"]);
        Assert.Equal(true, actual["data-filter-applied"]);
        Assert.Equal("width: 10px", actual["style"]);
        Assert.Equal("text", actual["data-filter"]);
        Assert.Equal("name", actual["data-name"]);
        Assert.Equal(9, actual.Count);
    }
}
