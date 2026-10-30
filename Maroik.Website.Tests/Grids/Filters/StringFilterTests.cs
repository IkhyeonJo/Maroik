using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Tests.Grids.Filters;

public class StringFilterTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Apply_StartsWith_NullOrEmptyValue_ReturnsNull(String? value)
    {
        Expression<Func<GridModel, String?>> expression = model => model.Name;

        Assert.Null(new StringFilter { Method = "starts-with", Values = new[] { value, "1" } }.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_StartsWith_UsingOriginalCaseFilter()
    {
        StringFilter filter = new() { Method = "starts-with", Values = "tes", Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "ttes" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TTESTE" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.StartsWith("tes"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_StartsWith_UsingUpperCaseFilter()
    {
        StringFilter filter = new() { Method = "starts-with", Values = "tt", Case = GridFilterCase.Upper };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "ttes" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TTESTE" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToUpper(CultureInfo.CurrentCulture).StartsWith("TT"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_StartsWith_UsingLowerCaseFilter()
    {
        StringFilter filter = new() { Method = "starts-with", Values = "TE", Case = GridFilterCase.Lower };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TTESTE" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToLower(CultureInfo.CurrentCulture).StartsWith("te"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_StartsWith_MultiFilter()
    {
        StringFilter filter = new() { Method = "starts-with", Values = new[] { "te", "TT" }, Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "ttes" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TTESTE" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && (model.Name.StartsWith("te") || model.Name.StartsWith("TT")));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Apply_EndsWith_NullOrEmptyValue_ReturnsNull(String? value)
    {
        Expression<Func<GridModel, String?>> expression = model => model.Name;

        Assert.Null(new StringFilter { Method = "ends-with", Values = new[] { value, "1" } }.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_EndsWith_UsingOriginalCaseFilter()
    {
        StringFilter filter = new() { Method = "ends-with", Values = "est", Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TESTEr" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.EndsWith("est"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_EndsWith_UsingUpperCaseFilter()
    {
        StringFilter filter = new() { Method = "ends-with", Values = "est", Case = GridFilterCase.Upper };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TESTEr" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToUpper(CultureInfo.CurrentCulture).EndsWith("EST"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_EndsWith_UsingLowerCaseFilter()
    {
        StringFilter filter = new() { Method = "ends-with", Values = "EST", Case = GridFilterCase.Lower };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TESTEr" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToLower(CultureInfo.CurrentCulture).EndsWith("est"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_EndsWith_MultiFilter()
    {
        StringFilter filter = new() { Method = "ends-with", Values = new[] { "t", "Er" }, Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" },
            new GridModel { Name = "TESTE" },
            new GridModel { Name = "TESTEr" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && (model.Name.EndsWith('t') || model.Name.EndsWith("Er")));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Apply_Contains_NullOrEmptyValue_ReturnsNull(String? value)
    {
        Expression<Func<GridModel, String?>> expression = model => model.Name;

        Assert.Null(new StringFilter { Method = "contains", Values = new[] { value, "1" } }.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_Contains_UsingOriginalCaseFilter()
    {
        StringFilter filter = new() { Method = "contains", Values = "es", Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.Contains("es"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Contains_UsingUpperCaseFilter()
    {
        StringFilter filter = new() { Method = "contains", Values = "es", Case = GridFilterCase.Upper };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToUpper(CultureInfo.CurrentCulture).Contains("ES"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Contains_UsingLowerCaseFilter()
    {
        StringFilter filter = new() { Method = "contains", Values = "ES", Case = GridFilterCase.Lower };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToLower(CultureInfo.CurrentCulture).Contains("es"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Contains_MultiFilter()
    {
        StringFilter filter = new() { Method = "contains", Values = new[] { "Te", "es" }, Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && (model.Name.Contains("Te") || model.Name.Contains("es")));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Consists_AllEmptyValues_ReturnsNull()
    {
        Expression<Func<GridModel, String?>> expression = model => model.Name;

        Assert.Null(new StringFilter { Method = "consists-of", Values = new[] { null, "", " " } }.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_Consists_UsingOriginalCaseFilter()
    {
        StringFilter filter = new() { Method = "consists-of", Values = "es T", Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.Contains('T') && model.Name.Contains("es"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Consists_UsingUpperCaseFilter()
    {
        StringFilter filter = new() { Method = "consists-of", Values = "es t", Case = GridFilterCase.Upper };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToUpper(CultureInfo.CurrentCulture).Contains('T') && model.Name.ToUpper(CultureInfo.CurrentCulture).Contains("ES"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Consists_UsingLowerCaseFilter()
    {
        StringFilter filter = new() { Method = "consists-of", Values = "ES T", Case = GridFilterCase.Lower };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToLower(CultureInfo.CurrentCulture).Contains('t') && model.Name.ToLower(CultureInfo.CurrentCulture).Contains("es"));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Consists_SkippingEmptyValues()
    {
        StringFilter filter = new() { Method = "consists-of", Values = new[] { null, "", "  ", " e s \t " }, Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.Contains('e') && model.Name.Contains('s'));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Consists_MultiFilter()
    {
        StringFilter filter = new() { Method = "consists-of", Values = new[] { "Te s", "es t" }, Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "Tes" },
            new GridModel { Name = "test" },
            new GridModel { Name = "TEST" }
        }.AsQueryable();

        IQueryable expected = items.Where(model =>
            model.Name != null &&
            (model.Name.Contains('t') && model.Name.Contains("es") ||
            model.Name.Contains("Te") && model.Name.Contains("Te")));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(GridFilterCase.Lower)]
    [InlineData(GridFilterCase.Upper)]
    [InlineData(GridFilterCase.Original)]
    public void Apply_NotEquals_FiltersNotEmptyAndNotNullValues(GridFilterCase filterCase)
    {
        StringFilter filter = new() { Method = "not-equals", Values = "", Case = filterCase };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "" },
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => !String.IsNullOrEmpty(model.Name));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_NotEquals_UsingOriginalCaseFilter()
    {
        StringFilter filter = new() { Method = "not-equals", Values = "test", Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != "test");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_NotEquals_UsingUpperCaseFilter()
    {
        StringFilter filter = new() { Method = "not-equals", Values = "test", Case = GridFilterCase.Upper };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name == null || model.Name.ToUpper(CultureInfo.CurrentCulture) != "TEST");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_NotEquals_UsingLowerCaseFilter()
    {
        StringFilter filter = new() { Method = "not-equals", Values = "TEST", Case = GridFilterCase.Lower };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name == null || model.Name.ToLower(CultureInfo.CurrentCulture) != "test");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_NotEquals_MultiFilter()
    {
        StringFilter filter = new() { Method = "not-equals", Values = new[] { "TEST2", null }, Case = GridFilterCase.Lower };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "" },
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => !String.IsNullOrEmpty(model.Name) && model.Name != "Test2");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(GridFilterCase.Lower)]
    [InlineData(GridFilterCase.Upper)]
    [InlineData(GridFilterCase.Original)]
    public void Apply_Equals_FiltersEmptyOrNullValues(GridFilterCase filterCase)
    {
        StringFilter filter = new() { Method = "equals", Values = "", Case = filterCase };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "" },
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => String.IsNullOrEmpty(model.Name));
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Equals_UsingOriginalCaseFilter()
    {
        StringFilter filter = new() { Method = "equals", Values = "test", Case = GridFilterCase.Original };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name == "test");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Equals_UsingUpperCaseFilter()
    {
        StringFilter filter = new() { Method = "equals", Values = "test", Case = GridFilterCase.Upper };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToUpper(CultureInfo.CurrentCulture) == "TEST");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Equals_UsingLowerCaseFilter()
    {
        StringFilter filter = new() { Method = "equals", Values = "TEST", Case = GridFilterCase.Lower };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => model.Name != null && model.Name.ToLower(CultureInfo.CurrentCulture) == "test");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_Equals_MultiFilter()
    {
        StringFilter filter = new() { Method = "equals", Values = new[] { "test2", "" }, Case = GridFilterCase.Upper };

        IQueryable<GridModel> items = new[]
        {
            new GridModel { Name = "" },
            new GridModel { Name = null },
            new GridModel { Name = "test" },
            new GridModel { Name = "Test" },
            new GridModel { Name = "Test2" }
        }.AsQueryable();

        IQueryable expected = items.Where(model => String.IsNullOrEmpty(model.Name) || model.Name == "Test2");
        IQueryable actual = items.Where(model => model.Name, filter);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Apply_EmptyValue_ReturnsNull()
    {
        StringFilter filter = new() { Method = "equals", Values = StringValues.Empty };
        Expression<Func<GridModel, String?>> expression = (model) => model.Name;

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }

    [Fact]
    public void Apply_BadMethod_ReturnsNull()
    {
        StringFilter filter = new() { Method = "test", Values = new[] { "a", "b" } };
        Expression<Func<GridModel, String?>> expression = (model) => model.Name;

        Assert.Null(filter.Apply(expression.Body, CultureInfo.CurrentCulture));
    }
}
