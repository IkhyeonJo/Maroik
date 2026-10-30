using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using Maroik.Website.Grids;
using Maroik.Website.Grids.Columns;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Processors;
using Maroik.Website.Grids.Rows;
using Maroik.Website.Grids.Sorts;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.AspNetCore.Html;
using Moq;

namespace Maroik.Website.Tests.Grids.Columns;

public class GridColumnTests
{
    private GridColumn<GridModel, Object?> column;

    public GridColumnTests()
    {
        Grid<GridModel> grid = new([]);
        column = new GridColumn<GridModel, Object?>(grid, model => model.Name);
    }

    [Fact]
    public void IGridColumn_ReturnsSort()
    {
        IGridColumn gridColumn = column;

        Assert.Equal(column.Sort, gridColumn.Sort);
    }

    [Fact]
    public void IGridColumnOfT_ReturnsSort()
    {
        IGridColumn<GridModel> gridColumn = column;

        Assert.Equal(column.Sort, gridColumn.Sort);
    }

    [Fact]
    public void IGridColumn_ReturnsFilter()
    {
        IGridColumn gridColumn = column;

        Assert.Equal(column.Filter, gridColumn.Filter);
    }

    [Fact]
    public void GridColumn_EmptyStyle()
    {
        Assert.Empty(new GridColumn<GridModel, Int32>(column.Grid, _ => 1).Style);
    }

    [Fact]
    public void GridColumn_SetsGrid()
    {
        Object actual = new GridColumn<GridModel, Int32>(column.Grid, _ => 0).Grid;
        Object expected = column.Grid;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GridColumn_EmptyCssClasses()
    {
        Assert.Empty(new GridColumn<GridModel, Int32>(column.Grid, _ => 1).CssClasses);
    }

    [Fact]
    public void GridColumn_SetsIsEncoded()
    {
        Assert.True(new GridColumn<GridModel, Int32>(column.Grid, _ => 1).IsEncoded);
    }

    [Fact]
    public void GridColumn_SetsExpression()
    {
        Object actual = new GridColumn<GridModel, Object?>(column.Grid, column.Expression).Expression;
        Object expected = column.Expression;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void GridColumn_NotMemberExpression_SetsEmptyTitle()
    {
        Assert.Empty(new GridColumn<GridModel, Int32>(column.Grid, _ => 1).Title.ToString()!);
    }

    [Fact]
    public void GridColumn_NoDisplayAttribute_SetsEmptyTitle()
    {
        Assert.Empty(new GridColumn<GridModel, Object?>(column.Grid, model => model.Name).Title.ToString()!);
    }

    [Fact]
    public void GridColumn_DisplayAttribute_SetsTitleFromDisplayName()
    {
        DisplayAttribute? display = typeof(GridModel).GetProperty(nameof(GridModel.Text))?.GetCustomAttribute<DisplayAttribute>();
        column = new GridColumn<GridModel, Object?>(column.Grid, model => model.Text);

        String? actual = column.Title.ToString();
        String? expected = display?.GetName();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GridColumn_DisplayAttribute_SetsTitleFromDisplayShortName()
    {
        DisplayAttribute? display = typeof(GridModel).GetProperty(nameof(GridModel.ShortText))?.GetCustomAttribute<DisplayAttribute>();
        column = new GridColumn<GridModel, Object?>(column.Grid, model => model.ShortText);

        String? expected = display?.GetShortName();
        String? actual = column.Title.ToString();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GridColumn_SetsExpressionValue()
    {
        GridModel model = new() { Name = "Testing name" };

        Assert.Equal("Testing name", column.ExpressionValue(model));
    }

    [Fact]
    public void GridColumn_SetsPreProcessorType()
    {
        GridProcessorType actual = new GridColumn<GridModel, Object>(column.Grid, _ => 0).ProcessorType;
        GridProcessorType expected = GridProcessorType.Pre;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GridColumn_SetsDefaultSort()
    {
        column = new GridColumn<GridModel, Object?>(column.Grid, model => model.Name);

        IGridColumnSort<GridModel, Object?> actual = column.Sort;

        Assert.Equal(GridSortOrder.Asc, actual.FirstOrder);
        Assert.Same(column, actual.Column);
        Assert.Null(actual.IsEnabled);
        Assert.Null(actual.Order);
    }

    [Fact]
    public void GridColumn_SetsNameForNonMemberExpression()
    {
        Assert.Empty(new GridColumn<GridModel, String?>(column.Grid, model => model.ToString()).Name);
    }

    [Fact]
    public void GridColumn_SetsNameFromExpression()
    {
        Assert.Equal("Child.NSum", new GridColumn<GridModel, Int32?>(column.Grid, model => model.Child!.NSum).Name);
    }

    [Fact]
    public void GridColumn_SetsDefaultFilter()
    {
        column = new GridColumn<GridModel, Object?>(column.Grid, model => model.Name);

        IGridColumnFilter<GridModel, Object?> actual = column.Filter;

        Assert.Equal("default", actual.Name);
        Assert.Equal(column, actual.Column);
        Assert.Null(actual.IsEnabled);
        Assert.Null(actual.Operator);
        Assert.Null(actual.Second);
        Assert.Null(actual.First);
        Assert.Null(actual.Type);
    }

    [Fact]
    public void Process_FilteredItems()
    {
        column.Filter = Fake.Of<IGridColumnFilter<GridModel, Object?>>();

        IQueryable<GridModel> filtered = new GridModel[2].AsQueryable();
        IQueryable<GridModel> items = new GridModel[2].AsQueryable();

        Mock.Get(column.Filter).Setup(mock => mock.Apply(items)).Returns(filtered);

        Object actual = column.Process(items);
        Object expected = filtered;

        Assert.Same(expected, actual);
    }

    [Fact]
    public void ValueFor_NullReferenceInExpressionValue_ReturnsEmpty()
    {
        column.ExpressionValue = model => model.Name;

        Assert.Empty(column.ValueFor(new GridRow<Object>(new GridModel(), 0)).ToString()!);
    }

    [Fact]
    public void ValueFor_NullReferenceInRenderValue_ReturnsEmpty()
    {
        column.RenderValue = (model, _) => model.Name;

        Assert.Empty(column.ValueFor(new GridRow<Object>(new GridModel(), 0)).ToString()!);
    }

    [Fact]
    public void ValueFor_NullRelationReference_ReturnsEmpty()
    {
        column.ExpressionValue = model => model.Child!.Name;

        Assert.Empty(column.ValueFor(new GridRow<Object>(new GridModel(), 0)).ToString()!);
    }

    [Fact]
    public void ValueFor_ExpressionValue_ThrowsNotNullReferenceException()
    {
        column.ExpressionValue = (_) => Int32.Parse("Zero", CultureInfo.InvariantCulture);

        Assert.Throws<FormatException>(() => column.ValueFor(new GridRow<Object>(new GridModel(), 0)));
    }

    [Fact]
    public void ValueFor_RenderValue_ThrowsNotNullReferenceException()
    {
        column.RenderValue = (_, _) => Int32.Parse("Zero", CultureInfo.InvariantCulture);

        Assert.Throws<FormatException>(() => column.ValueFor(new GridRow<Object>(new GridModel(), 0)));
    }

    [Theory]
    [InlineData(null, "For {0}", true, "")]
    [InlineData(null, "For {0}", false, "")]
    [InlineData("<name>", null, true, "<name>")]
    [InlineData("<name>", null, false, "<name>")]
    [InlineData("<name>", "For <{0}>", true, "<name>")]
    [InlineData("<name>", "For <{0}>", false, "<name>")]
    public void ValueFor_RenderValue_Html(String? value, String? format, Boolean isEncoded, String renderedValue)
    {
        GridRow<GridModel> row = new(new GridModel { Content = value == null ? null : new HtmlString(value) }, 0);
        column.RenderValue = (model, _) => model.Content;
        column.ExpressionValue = (_) => "";
        column.IsEncoded = isEncoded;
        column.Format = format;

        Assert.Equal(renderedValue, column.ValueFor(row).ToString());
    }

    [Fact]
    public void ValueFor_RenderValue_Index()
    {
        GridRow<GridModel> row = new(new GridModel { Name = "Test" }, 33);
        column.RenderValue = (model, i) => model.Name + " " + i;
        column.ExpressionValue = (_) => "";

        Assert.Equal("Test 33", column.ValueFor(row).ToString());
    }

    [Theory]
    [InlineData(null, "For {0}", true, "")]
    [InlineData(null, "For {0}", false, "")]
    [InlineData("<name>", null, true, "<name>")]
    [InlineData("<name>", null, false, "<name>")]
    [InlineData("<name>", "For <{0}>", true, "<name>")]
    [InlineData("<name>", "For <{0}>", false, "<name>")]
    public void ValueFor_ExpressionValue_Html(String? value, String? format, Boolean isEncoded, String expressionValue)
    {
        GridRow<GridModel> row = new(new GridModel { Content = value == null ? null : new HtmlString(value) }, 0);
        column = new GridColumn<GridModel, Object?>(column.Grid, model => model.Content);
        column.IsEncoded = isEncoded;
        column.Format = format;

        Assert.Equal(expressionValue, column.ValueFor(row).ToString());
    }

    [Theory]
    [InlineData(null, "For {0}", true, "")]
    [InlineData(null, "For {0}", false, "")]
    [InlineData("<name>", null, false, "<name>")]
    [InlineData("<name>", null, true, "&lt;name&gt;")]
    [InlineData("<name>", "For <{0}>", false, "For <<name>>")]
    [InlineData("<name>", "For <{0}>", true, "For &lt;&lt;name&gt;&gt;")]
    public void ValueFor_RenderValue(String? value, String? format, Boolean isEncoded, String renderedValue)
    {
        using StringWriter writer = new();
        GridRow<GridModel> row = new(new GridModel { Name = value }, 33);

        column.RenderValue = (model, _) => model.Name;
        column.ExpressionValue = (_) => "";
        column.IsEncoded = isEncoded;
        column.Format = format;

        column.ValueFor(row).WriteTo(writer, HtmlEncoder.Default);

        Assert.Equal(renderedValue, writer.ToString());
    }

    [Fact]
    public void ValueFor_BadValue_EnumExpressionValue()
    {
        GridRow<GridModel> row = new(new GridModel { Enum = (TestEnum)100 }, 0);
        GridColumn<GridModel, TestEnum> enumColumn = new(column.Grid, model => model.Enum);

        Assert.Equal(row.Model.Enum.ToString(), enumColumn.ValueFor(row).ToString());
    }

    [Theory]
    [InlineData(TestEnum.First, "1st")]
    [InlineData(TestEnum.Second, "2nd")]
    [InlineData(TestEnum.Third, nameof(TestEnum.Third))]
    [InlineData(TestEnum.Fourth, nameof(TestEnum.Fourth))]
    public void ValueFor_NullableEnumExpressionValue(TestEnum value, String expressionValue)
    {
        GridRow<GridModel> row = new(new GridModel { Enum = value }, 0);
        GridColumn<GridModel, TestEnum?> enumColumn = new(column.Grid, model => model.Enum);

        Assert.Equal(expressionValue, enumColumn.ValueFor(row).ToString());
    }

    [Theory]
    [InlineData(TestEnum.First, "1st")]
    [InlineData(TestEnum.Second, "2nd")]
    [InlineData(TestEnum.Third, nameof(TestEnum.Third))]
    [InlineData(TestEnum.Fourth, nameof(TestEnum.Fourth))]
    public void ValueFor_EnumExpressionValue(TestEnum value, String expressionValue)
    {
        GridRow<GridModel> row = new(new GridModel { Enum = value }, 0);
        GridColumn<GridModel, TestEnum> enumColumn = new(column.Grid, model => model.Enum);

        Assert.Equal(expressionValue, enumColumn.ValueFor(row).ToString());
    }

    [Fact]
    public void ValueFor_NullEnum()
    {
        GridRow<GridModel> row = new(new GridModel(), 0);
        GridColumn<GridModel, TestEnum?> enumColumn = new(column.Grid, model => model.NEnum);

        Assert.Empty(enumColumn.ValueFor(row).ToString()!);
    }

    [Theory]
    [InlineData(null, "For {0}", true, "")]
    [InlineData(null, "For {0}", false, "")]
    [InlineData("<name>", null, false, "<name>")]
    [InlineData("<name>", null, true, "&lt;name&gt;")]
    [InlineData("<name>", "For <{0}>", false, "For <<name>>")]
    [InlineData("<name>", "For <{0}>", true, "For &lt;&lt;name&gt;&gt;")]
    public void ValueFor_ExpressionValue(String? value, String? format, Boolean isEncoded, String expressionValue)
    {
        using StringWriter writer = new();
        GridRow<GridModel> row = new(new GridModel { Name = value }, 0);

        column.IsEncoded = isEncoded;
        column.Format = format;

        column.ValueFor(row).WriteTo(writer, HtmlEncoder.Default);

        Assert.Equal(expressionValue, writer.ToString());
    }
}
