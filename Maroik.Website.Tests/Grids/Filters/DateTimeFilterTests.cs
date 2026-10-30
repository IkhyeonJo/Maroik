using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Tests.Grids.Filters;

public class DateTimeFilterTests
{
    private IQueryable<GridModel> items;
    private DateTimeFilter filter;

    public DateTimeFilterTests()
    {
        items = new[]
        {
            new GridModel { Date = new DateTime(2013, 01, 01), NDate = null },
            new GridModel { Date = new DateTime(2014, 07, 13), NDate = new DateTime(2015, 01, 01) },
            new GridModel { Date = new DateTime(2015, 01, 01), NDate = new DateTime(2014, 07, 13) }
        }.AsQueryable();

        filter = new DateTimeFilter();
    }

    [Fact]
    public void Apply_BadDateOnlyValue_ReturnsItems()
    {
        Expression<Func<GridModel, DateOnly>> expression = model => model.DateOnly;
        filter.Values = "Test";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_BadDateTimeValue_ReturnsItems()
    {
        Expression<Func<GridModel, DateTime>> expression = model => model.Date;
        filter.Values = "Test";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_BadTimeOnlyValue_ReturnsItems()
    {
        Expression<Func<GridModel, TimeOnly>> expression = model => model.TimeOnly;
        filter.Values = "Test";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_NullableEqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate == (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableEqualsFilter()
    {
        filter.Method = "equals";
        filter.Values = new[] { "", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate == null || model.NDate == new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_EqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date == (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleEqualsFilter()
    {
        filter.Method = "equals";
        filter.Values = new[] { "2013-01-01", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date == new DateTime(2013, 1, 1) || model.Date == new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_NullableNotEqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate != (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableNotEqualsFilter()
    {
        filter.Method = "not-equals";
        filter.Values = new[] { "", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate != null && model.NDate != new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_NotEqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date != (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNotEqualsFilter()
    {
        filter.Method = "not-equals";
        filter.Values = new[] { "2013-01-01", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date != new DateTime(2013, 1, 1) && model.Date != new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_NullableEarlierThanFilter(String value)
    {
        filter.Values = value;
        filter.Method = "earlier-than";

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate < (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableEarlierThanFilter()
    {
        filter.Method = "earlier-than";
        filter.Values = new[] { "", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate < new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_EarlierThanFilter(String value)
    {
        filter.Values = value;
        filter.Method = "earlier-than";

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date < (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleEarlierThanFilter()
    {
        filter.Method = "earlier-than";
        filter.Values = new[] { "2013-01-01", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date < new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_NullableLaterThanFilter(String value)
    {
        filter.Values = value;
        filter.Method = "later-than";

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate > (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableLaterThanFilter()
    {
        filter.Method = "later-than";
        filter.Values = new[] { "", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate > new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_LaterThanFilter(String value)
    {
        filter.Values = value;
        filter.Method = "later-than";

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date > (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleLaterThanFilter()
    {
        filter.Method = "later-than";
        filter.Values = new[] { "2013-01-01", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date > new DateTime(2013, 1, 1));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_NullableEarlierThanOrEqualFilter(String value)
    {
        filter.Values = value;
        filter.Method = "earlier-than-or-equal";

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate <= (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableEarlierThanOrEqualFilter()
    {
        filter.Method = "earlier-than-or-equal";
        filter.Values = new[] { "", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate <= new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_EarlierThanOrEqualFilter(String value)
    {
        filter.Values = value;
        filter.Method = "earlier-than-or-equal";

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date <= (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleEarlierThanOrEqualFilter()
    {
        filter.Method = "earlier-than-or-equal";
        filter.Values = new[] { "2013-01-01", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date <= new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_NullableLaterThanOrEqualFilter(String value)
    {
        filter.Values = value;
        filter.Method = "later-than-or-equal";

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate >= (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableLaterThanOrEqualFilter()
    {
        filter.Method = "later-than-or-equal";
        filter.Values = new[] { "", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate >= new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("2014-07-13")]
    public void Apply_LaterThanOrEqualFilter(String value)
    {
        filter.Values = value;
        filter.Method = "later-than-or-equal";

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date >= (String.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.CurrentCulture)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleLaterThanOrEqualFilter()
    {
        filter.Method = "later-than-or-equal";
        filter.Values = new[] { "", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.Date, filter);
        IEnumerable expected = items.Where(model => model.Date >= new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_FilterBasedOnCulture()
    {
        Expression<Func<GridModel, DateTime>> expression = model => model.Date;

        filter.Method = "equals";
        filter.Values = new DateTime(2014, 7, 13).ToString("d", new CultureInfo("bg-BG"));

        IEnumerable expected = items.Where(model => model.Date == new DateTime(2014, 7, 13));
        IEnumerable actual = items.Where(Expression.Lambda<Func<GridModel, Boolean>>(filter.Apply(expression.Body, new CultureInfo("bg-BG"))!, expression.Parameters[0]));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleWithBadValues()
    {
        filter.Method = "equals";
        filter.Values = new[] { "", "test", "2014-07-13" };

        IEnumerable actual = items.Where(model => model.NDate, filter);
        IEnumerable expected = items.Where(model => model.NDate == null || model.NDate == new DateTime(2014, 7, 13));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_EmptyValue_ReturnsNull()
    {
        Expression<Func<GridModel, DateTime>> expression = model => model.Date;
        filter.Values = StringValues.Empty;
        filter.Method = "equals";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_BadMethod_ReturnsNull()
    {
        Expression<Func<GridModel, DateTime>> expression = model => model.Date;
        filter.Values = "2014-07-13";
        filter.Method = "test";

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_BadCultureValue_ReturnsNull()
    {
        Expression<Func<GridModel, DateTime>> expression = model => model.Date;
        filter.Values = "13.07.2013";
        filter.Method = "equals";

        Assert.Null(filter.Apply(expression.Body, new CultureInfo("en-US")));
    }
}
