using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Tests.Grids.Filters;

public class GuidFilterTests
{
    private Expression<Func<GridModel, Guid?>> nGuidExpression;
    private Expression<Func<GridModel, Guid>> guidExpression;
    private IQueryable<GridModel> items;
    private GuidFilter filter;

    public GuidFilterTests()
    {
        items = new[]
        {
            new GridModel { Guid = new Guid("bf64a86e-0b70-4430-99f6-8dd947e64947"), NGuid = null },
            new GridModel { Guid = new Guid("bf64a86e-0b70-4430-99f6-8dd947e64948"), NGuid = new Guid("bfce0004-8af9-4f28-99d9-ea24b58b9588") },
            new GridModel { Guid = new Guid("bf64a86e-0b70-4430-99f6-8dd947e64949"), NGuid = new Guid("bfce0004-8af9-4f28-99d9-ea24b58b9589") }
        }.AsQueryable();

        nGuidExpression = model => model.NGuid;
        guidExpression = model => model.Guid;
        filter = new GuidFilter();
    }

    [Fact]
    public void Apply_BadValue_ReturnsItems()
    {
        filter.Values = "Test";

        Assert.Null(filter.Apply(guidExpression.Body, CultureInfo.CurrentCulture));
    }

    [Theory]
    [InlineData("")]
    [InlineData("bfce0004-8af9-4f28-99d9-ea24b58b9588")]
    public void Apply_NullableEqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(nGuidExpression, filter);
        IEnumerable expected = items.Where(model => model.NGuid == (String.IsNullOrEmpty(value) ? null : Guid.Parse(value)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableEqualsFilter()
    {
        filter.Method = "equals";
        filter.Values = new[] { "", "bf64a86e-0b70-4430-99f6-8dd947e64948" };

        IEnumerable expected = items.Where(model => model.NGuid == null || model.NGuid == Guid.Parse("bf64a86e-0b70-4430-99f6-8dd947e64947"));
        IEnumerable actual = items.Where(nGuidExpression, filter);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bf64a86e-0b70-4430-99f6-8dd947e64948")]
    public void Apply_EqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "equals";

        IEnumerable actual = items.Where(guidExpression, filter);
        IEnumerable expected = items.Where(model => model.Guid == (String.IsNullOrEmpty(value) ? null : Guid.Parse(value)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleEqualsFilter()
    {
        filter.Method = "equals";
        filter.Values = new[] { "bfce0004-8af9-4f28-99d9-ea24b58b9588", "bf64a86e-0b70-4430-99f6-8dd947e64948" };

        IEnumerable actual = items.Where(guidExpression, filter);
        IEnumerable expected = items.Where(model =>
            model.Guid == Guid.Parse("bfce0004-8af9-4f28-99d9-ea24b58b9588") ||
            model.Guid == Guid.Parse("bf64a86e-0b70-4430-99f6-8dd947e64948"));

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bf64a86e-0b70-4430-99f6-8dd947e64948")]
    public void Apply_NullableNotEqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(nGuidExpression, filter);
        IEnumerable expected = items.Where(model => model.NGuid != (String.IsNullOrEmpty(value) ? null : Guid.Parse(value)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNullableNotEqualsFilter()
    {
        filter.Method = "not-equals";
        filter.Values = new[] { "", "bf64a86e-0b70-4430-99f6-8dd947e64948" };

        IEnumerable expected = items.Where(model => model.NGuid != null && model.NGuid != Guid.Parse("bf64a86e-0b70-4430-99f6-8dd947e64947"));
        IEnumerable actual = items.Where(nGuidExpression, filter);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData("bf64a86e-0b70-4430-99f6-8dd947e64948")]
    public void Apply_NotEqualsFilter(String value)
    {
        filter.Values = value;
        filter.Method = "not-equals";

        IEnumerable actual = items.Where(guidExpression, filter);
        IEnumerable expected = items.Where(model => model.Guid != (String.IsNullOrEmpty(value) ? null : Guid.Parse(value)));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleNotEqualsFilter()
    {
        filter.Method = "not-equals";
        filter.Values = new[] { "bfce0004-8af9-4f28-99d9-ea24b58b9588", "bf64a86e-0b70-4430-99f6-8dd947e64948" };

        IEnumerable actual = items.Where(guidExpression, filter);
        IEnumerable expected = items.Where(model =>
            model.Guid != Guid.Parse("bfce0004-8af9-4f28-99d9-ea24b58b9588") &&
            model.Guid != Guid.Parse("bf64a86e-0b70-4430-99f6-8dd947e64948"));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_MultipleWithBadValues()
    {
        filter.Method = "equals";
        filter.Values = new[] { "", "test", "bf64a86e-0b70-4430-99f6-8dd947e64948" };

        IEnumerable expected = items.Where(model => model.NGuid == null || model.NGuid == Guid.Parse("bf64a86e-0b70-4430-99f6-8dd947e64947"));
        IEnumerable actual = items.Where(nGuidExpression, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_EmptyValue_ReturnsNull()
    {
        filter.Method = "equals";
        filter.Values = StringValues.Empty;

        Assert.Null(filter.Apply(guidExpression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_BadMethod_ReturnsNull()
    {
        filter.Method = "test";
        filter.Values = "bf64a86e-0b70-4430-99f6-8dd947e64948";

        Assert.Null(filter.Apply(guidExpression.Body, CultureInfo.CurrentCulture));
    }
}
