using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Tests.Grids.Filters;

public class BooleanFilterTests
{
    private BooleanFilter filter;
    private IQueryable<GridModel> items;
    private Expression<Func<GridModel, Boolean>> expression;

    public BooleanFilterTests()
    {
        items = new[]
        {
            new GridModel(),
            new GridModel { IsChecked = true, NIsChecked = false },
            new GridModel { IsChecked = false, NIsChecked = true }
        }.AsQueryable();

        filter = new BooleanFilter();
        expression = model => model.IsChecked;
    }

    [Fact]
    public void Apply_BadValue_ReturnsNull()
    {
        filter.Values = "Test";
        filter.Method = "equals";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    public void Apply_NullableEqualsFilter(String value, Boolean? isChecked)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(model => model.NIsChecked, filter);
        IEnumerable expected = items.Where(model => model.NIsChecked == isChecked);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableEqualsFilter()
    {
        filter.Method = "equals";
        filter.Values = new[] { "", "false" };

        IEnumerable actual = items.Where(model => model.NIsChecked, filter);
        IEnumerable expected = items.Where(model => model.NIsChecked != true);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    public void Apply_EqualsFilter(String value, Boolean? isChecked)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(expression, filter);
        IEnumerable expected = items.Where(model => model.IsChecked == isChecked);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleEqualsFilter()
    {
        filter.Method = "equals";
        filter.Values = new[] { "true", "false" };

        IEnumerable expected = items;
        IEnumerable actual = items.Where(expression, filter);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    public void Apply_NullableNotEqualsFilter(String value, Boolean? isChecked)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(model => model.NIsChecked, filter);
        IEnumerable expected = items.Where(model => model.NIsChecked != isChecked);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableNotEqualsFilter()
    {
        filter.Method = "not-equals";
        filter.Values = new[] { "", "false" };

        IEnumerable actual = items.Where(model => model.NIsChecked, filter);
        IEnumerable expected = items.Where(model => model.NIsChecked == true);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    [InlineData("false", false)]
    [InlineData("FALSE", false)]
    public void Apply_NotEqualsFilter(String value, Boolean? isChecked)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(expression, filter);
        IEnumerable expected = items.Where(model => model.IsChecked != isChecked);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNotEqualsFilter()
    {
        filter.Method = "not-equals";
        filter.Values = new[] { "true", "false" };

        Assert.Empty(items.Where(expression, filter));
    }

    [Fact]
    public void Apply_MultipleWithBadValues()
    {
        filter.Method = "equals";
        filter.Values = new[] { "", "test", "false" };

        IEnumerable actual = items.Where(model => model.NIsChecked, filter);
        IEnumerable expected = items.Where(model => model.NIsChecked != true);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_EmptyValue_ReturnsNull()
    {
        filter.Method = "equals";
        filter.Values = StringValues.Empty;

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_BadMethod_ReturnsNull()
    {
        filter.Method = "test";
        filter.Values = "false";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }
}
