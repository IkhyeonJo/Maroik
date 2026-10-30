using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Tests.Grids.Filters;

public class EnumFilterTests
{
    private Expression<Func<GridModel, TestEnum>> expression;
    private IQueryable<GridModel> items;
    private EnumFilter filter;

    public EnumFilterTests()
    {
        items = new[]
        {
            new GridModel(),
            new GridModel { Enum = TestEnum.First },
            new GridModel { Enum = TestEnum.Second },
            new GridModel { Enum = TestEnum.First, NEnum = TestEnum.Second },
            new GridModel { Enum = TestEnum.Second, NEnum = TestEnum.First }
        }.AsQueryable();

        expression = model => model.Enum;
        filter = new EnumFilter();
    }

    [Fact]
    public void Apply_BadValue_ReturnsNull()
    {
        filter.Values = "test";
        filter.Method = "equals";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("1", TestEnum.Second)]
    public void Apply_NullableEqualsFilter(String value, TestEnum? test)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(model => model.NEnum, filter);
        IEnumerable expected = items.Where(model => model.NEnum == test);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("1", TestEnum.Second)]
    public void Apply_EqualsFilter(String value, TestEnum? test)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(model => model.Enum, filter);
        IEnumerable expected = items.Where(model => model.Enum == test);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("1", TestEnum.Second)]
    public void Apply_NullableNotEqualsFilter(String value, TestEnum? test)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(model => model.NEnum, filter);
        IEnumerable expected = items.Where(model => model.NEnum != test);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("", null)]
    [InlineData("1", TestEnum.Second)]
    public void Apply_NotEqualsFilter(String value, TestEnum? test)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(model => model.Enum, filter);
        IEnumerable expected = items.Where(model => model.Enum != test);

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
        filter.Values = "0";
        filter.Method = "test";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }
}
