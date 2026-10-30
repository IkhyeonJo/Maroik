using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Tests.Grids.Fixtures;

namespace Maroik.Website.Tests.Grids.Filters;

public class GridEnumerableFilterTests
{
    private IQueryable<GridModel> items;

    public GridEnumerableFilterTests()
    {
        items = new[]
        {
            new GridModel { NullableArrayField = ["", "678", "retest", "58"] },
            new GridModel { NullableArrayField = ["10", "12", "33", "84", "58"] },
            new GridModel { NullableArrayField = [null, "test", "2", "3", "4", "5"] },
            new GridModel { NullableArrayField = [null, "TEST", "2", "3", "4", "5"] }
        }.AsQueryable();

        foreach (GridModel model in items)
        {
            model.NullableEnumerableField = model.NullableArrayField;
            model.NullableListField = model.NullableArrayField?.ToList();
        }
    }

    [Fact]
    public void Method_Set()
    {
        Assert.Equal("test", new EnumerableFilter<StringFilter> { Method = "test" }.Method);
    }

    [Fact]
    public void Values_Set()
    {
        Assert.Equal("test", new EnumerableFilter<StringFilter> { Values = "test" }.Values);
    }

    [Fact]
    public void Case_Set()
    {
        GridFilterCase actual = new EnumerableFilter<StringFilter> { Case = GridFilterCase.Upper }.Case;
        GridFilterCase expected = GridFilterCase.Upper;

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_NotEnumerable_ReturnsNull()
    {
        Expression<Func<GridModel, Type>> expression = (_) => typeof(String);

        Assert.Null(new EnumerableFilter<StringFilter>().Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_NoAppliedFilter_ReturnsNull()
    {
        Expression<Func<GridModel, IEnumerable<String>>> expression = (_) => Enumerable.Empty<String>();
        EnumerableFilter<StringFilter> filter = new()
        {
            Case = GridFilterCase.Original,
            Values = new[] { "" },
            Method = "contains"
        };

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_OnList()
    {
        EnumerableFilter<StringFilter> filter = new()
        {
            Values = new[] { "test", "33" },
            Case = GridFilterCase.Original,
            Method = "equals"
        };

        IEnumerable expected = items.Where(model => model.NullableListField!.Any(item => item == "test" || item == "33"));
        IEnumerable actual = items.Where(model => model.NullableListField, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_OnArray()
    {
        EnumerableFilter<StringFilter> filter = new()
        {
            Values = new[] { "test", "33" },
            Case = GridFilterCase.Original,
            Method = "equals"
        };

        IEnumerable expected = items.Where(model => model.NullableArrayField!.Any(item => item == "test" || item == "33"));
        IEnumerable actual = items.Where(model => model.NullableArrayField, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_OnEnumerable()
    {
        EnumerableFilter<StringFilter> filter = new()
        {
            Values = new[] { "test", "33" },
            Case = GridFilterCase.Original,
            Method = "equals"
        };

        IEnumerable expected = items.Where(model => model.NullableEnumerableField!.Any(item => item == "test" || item == "33"));
        IEnumerable actual = items.Where(model => model.NullableEnumerableField, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_UsingOriginalCaseFilter()
    {
        EnumerableFilter<StringFilter> filter = new()
        {
            Values = new[] { "test", "33" },
            Case = GridFilterCase.Original,
            Method = "equals"
        };

        IEnumerable expected = items.Where(model => model.NullableArrayField!.Any(item => item == "test" || item == "33"));
        IEnumerable actual = items.Where(model => model.NullableListField, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_UsingUpperCaseFilter()
    {
        EnumerableFilter<StringFilter> filter = new()
        {
            Values = new[] { "test", "33" },
            Case = GridFilterCase.Upper,
            Method = "equals"
        };

        IEnumerable expected = items.Where(model => model.NullableArrayField!.Any(item => item != null && item.ToUpper(CultureInfo.CurrentCulture) == "TEST" || item == "33"));
        IEnumerable actual = items.Where(model => model.NullableListField, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_UsingLowerCaseFilter()
    {
        EnumerableFilter<StringFilter> filter = new()
        {
            Values = new[] { "TEST", "33" },
            Case = GridFilterCase.Lower,
            Method = "equals"
        };

        IEnumerable expected = items.Where(model => model.NullableArrayField != null && model.NullableArrayField.Any(item => item != null && item.ToLower(CultureInfo.CurrentCulture) == "test" || item == "33"));
        IEnumerable actual = items.Where(model => model.NullableListField, filter);

        Assert.Equal(expected, actual);
    }
}
