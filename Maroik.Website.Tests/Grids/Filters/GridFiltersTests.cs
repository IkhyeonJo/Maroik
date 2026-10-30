using Maroik.Website.Grids;
using Maroik.Website.Grids.Columns;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Tests.Grids.Fixtures;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Moq;

namespace Maroik.Website.Tests.Grids.Filters;

public class GridFiltersTests
{
    private GridFilters filters;
    private IGridColumn<GridModel, String?> column;

    public GridFiltersTests()
    {
        filters = new GridFilters();
        Grid<GridModel> grid = new([]);
        column = new GridColumn<GridModel, String?>(grid, model => model.Name);
    }

    [Fact]
    public void GridFilters_SetEmptyBooleanText()
    {
        Assert.Empty(new GridFilters().BooleanEmptyOptionText());
    }

    [Fact]
    public void GridFilters_SetFalseBooleanText()
    {
        Assert.Equal("False", new GridFilters().BooleanFalseOptionText());
    }

    [Fact]
    public void GridFilters_SetTrueBooleanText()
    {
        Assert.Equal("True", new GridFilters().BooleanTrueOptionText());
    }

    [Theory]
    [InlineData(typeof(SByte), "equals", typeof(NumberFilter<SByte>))]
    [InlineData(typeof(SByte), "not-equals", typeof(NumberFilter<SByte>))]
    [InlineData(typeof(SByte), "less-than", typeof(NumberFilter<SByte>))]
    [InlineData(typeof(SByte), "greater-than", typeof(NumberFilter<SByte>))]
    [InlineData(typeof(SByte), "less-than-or-equal", typeof(NumberFilter<SByte>))]
    [InlineData(typeof(SByte), "greater-than-or-equal", typeof(NumberFilter<SByte>))]

    [InlineData(typeof(Byte), "equals", typeof(NumberFilter<Byte>))]
    [InlineData(typeof(Byte), "not-equals", typeof(NumberFilter<Byte>))]
    [InlineData(typeof(Byte), "less-than", typeof(NumberFilter<Byte>))]
    [InlineData(typeof(Byte), "greater-than", typeof(NumberFilter<Byte>))]
    [InlineData(typeof(Byte), "less-than-or-equal", typeof(NumberFilter<Byte>))]
    [InlineData(typeof(Byte), "greater-than-or-equal", typeof(NumberFilter<Byte>))]

    [InlineData(typeof(Int16), "equals", typeof(NumberFilter<Int16>))]
    [InlineData(typeof(Int16), "not-equals", typeof(NumberFilter<Int16>))]
    [InlineData(typeof(Int16), "less-than", typeof(NumberFilter<Int16>))]
    [InlineData(typeof(Int16), "greater-than", typeof(NumberFilter<Int16>))]
    [InlineData(typeof(Int16), "less-than-or-equal", typeof(NumberFilter<Int16>))]
    [InlineData(typeof(Int16), "greater-than-or-equal", typeof(NumberFilter<Int16>))]

    [InlineData(typeof(UInt16), "equals", typeof(NumberFilter<UInt16>))]
    [InlineData(typeof(UInt16), "not-equals", typeof(NumberFilter<UInt16>))]
    [InlineData(typeof(UInt16), "less-than", typeof(NumberFilter<UInt16>))]
    [InlineData(typeof(UInt16), "greater-than", typeof(NumberFilter<UInt16>))]
    [InlineData(typeof(UInt16), "less-than-or-equal", typeof(NumberFilter<UInt16>))]
    [InlineData(typeof(UInt16), "greater-than-or-equal", typeof(NumberFilter<UInt16>))]

    [InlineData(typeof(Int32), "equals", typeof(NumberFilter<Int32>))]
    [InlineData(typeof(Int32), "not-equals", typeof(NumberFilter<Int32>))]
    [InlineData(typeof(Int32), "less-than", typeof(NumberFilter<Int32>))]
    [InlineData(typeof(Int32), "greater-than", typeof(NumberFilter<Int32>))]
    [InlineData(typeof(Int32), "less-than-or-equal", typeof(NumberFilter<Int32>))]
    [InlineData(typeof(Int32), "greater-than-or-equal", typeof(NumberFilter<Int32>))]

    [InlineData(typeof(UInt32), "equals", typeof(NumberFilter<UInt32>))]
    [InlineData(typeof(UInt32), "not-equals", typeof(NumberFilter<UInt32>))]
    [InlineData(typeof(UInt32), "less-than", typeof(NumberFilter<UInt32>))]
    [InlineData(typeof(UInt32), "greater-than", typeof(NumberFilter<UInt32>))]
    [InlineData(typeof(UInt32), "less-than-or-equal", typeof(NumberFilter<UInt32>))]
    [InlineData(typeof(UInt32), "greater-than-or-equal", typeof(NumberFilter<UInt32>))]

    [InlineData(typeof(Int64), "equals", typeof(NumberFilter<Int64>))]
    [InlineData(typeof(Int64), "not-equals", typeof(NumberFilter<Int64>))]
    [InlineData(typeof(Int64), "less-than", typeof(NumberFilter<Int64>))]
    [InlineData(typeof(Int64), "greater-than", typeof(NumberFilter<Int64>))]
    [InlineData(typeof(Int64), "less-than-or-equal", typeof(NumberFilter<Int64>))]
    [InlineData(typeof(Int64), "greater-than-or-equal", typeof(NumberFilter<Int64>))]

    [InlineData(typeof(UInt64), "equals", typeof(NumberFilter<UInt64>))]
    [InlineData(typeof(UInt64), "not-equals", typeof(NumberFilter<UInt64>))]
    [InlineData(typeof(UInt64), "less-than", typeof(NumberFilter<UInt64>))]
    [InlineData(typeof(UInt64), "greater-than", typeof(NumberFilter<UInt64>))]
    [InlineData(typeof(UInt64), "less-than-or-equal", typeof(NumberFilter<UInt64>))]
    [InlineData(typeof(UInt64), "greater-than-or-equal", typeof(NumberFilter<UInt64>))]

    [InlineData(typeof(Single), "equals", typeof(NumberFilter<Single>))]
    [InlineData(typeof(Single), "not-equals", typeof(NumberFilter<Single>))]
    [InlineData(typeof(Single), "less-than", typeof(NumberFilter<Single>))]
    [InlineData(typeof(Single), "greater-than", typeof(NumberFilter<Single>))]
    [InlineData(typeof(Single), "less-than-or-equal", typeof(NumberFilter<Single>))]
    [InlineData(typeof(Single), "greater-than-or-equal", typeof(NumberFilter<Single>))]

    [InlineData(typeof(Double), "equals", typeof(NumberFilter<Double>))]
    [InlineData(typeof(Double), "not-equals", typeof(NumberFilter<Double>))]
    [InlineData(typeof(Double), "less-than", typeof(NumberFilter<Double>))]
    [InlineData(typeof(Double), "greater-than", typeof(NumberFilter<Double>))]
    [InlineData(typeof(Double), "less-than-or-equal", typeof(NumberFilter<Double>))]
    [InlineData(typeof(Double), "greater-than-or-equal", typeof(NumberFilter<Double>))]

    [InlineData(typeof(Decimal), "equals", typeof(NumberFilter<Decimal>))]
    [InlineData(typeof(Decimal), "not-equals", typeof(NumberFilter<Decimal>))]
    [InlineData(typeof(Decimal), "less-than", typeof(NumberFilter<Decimal>))]
    [InlineData(typeof(Decimal), "greater-than", typeof(NumberFilter<Decimal>))]
    [InlineData(typeof(Decimal), "less-than-or-equal", typeof(NumberFilter<Decimal>))]
    [InlineData(typeof(Decimal), "greater-than-or-equal", typeof(NumberFilter<Decimal>))]

    [InlineData(typeof(DateOnly), "equals", typeof(DateTimeFilter))]
    [InlineData(typeof(DateOnly), "not-equals", typeof(DateTimeFilter))]
    [InlineData(typeof(DateOnly), "earlier-than", typeof(DateTimeFilter))]
    [InlineData(typeof(DateOnly), "later-than", typeof(DateTimeFilter))]
    [InlineData(typeof(DateOnly), "earlier-than-or-equal", typeof(DateTimeFilter))]
    [InlineData(typeof(DateOnly), "later-than-or-equal", typeof(DateTimeFilter))]

    [InlineData(typeof(DateTime), "equals", typeof(DateTimeFilter))]
    [InlineData(typeof(DateTime), "not-equals", typeof(DateTimeFilter))]
    [InlineData(typeof(DateTime), "earlier-than", typeof(DateTimeFilter))]
    [InlineData(typeof(DateTime), "later-than", typeof(DateTimeFilter))]
    [InlineData(typeof(DateTime), "earlier-than-or-equal", typeof(DateTimeFilter))]
    [InlineData(typeof(DateTime), "later-than-or-equal", typeof(DateTimeFilter))]

    [InlineData(typeof(TimeOnly), "equals", typeof(DateTimeFilter))]
    [InlineData(typeof(TimeOnly), "not-equals", typeof(DateTimeFilter))]
    [InlineData(typeof(TimeOnly), "earlier-than", typeof(DateTimeFilter))]
    [InlineData(typeof(TimeOnly), "later-than", typeof(DateTimeFilter))]
    [InlineData(typeof(TimeOnly), "earlier-than-or-equal", typeof(DateTimeFilter))]
    [InlineData(typeof(TimeOnly), "later-than-or-equal", typeof(DateTimeFilter))]

    [InlineData(typeof(Boolean), "equals", typeof(BooleanFilter))]
    [InlineData(typeof(Boolean), "not-equals", typeof(BooleanFilter))]

    [InlineData(typeof(String), "equals", typeof(StringFilter))]
    [InlineData(typeof(String), "not-equals", typeof(StringFilter))]
    [InlineData(typeof(String), "contains", typeof(StringFilter))]
    [InlineData(typeof(String), "consists-of", typeof(StringFilter))]
    [InlineData(typeof(String), "ends-with", typeof(StringFilter))]
    [InlineData(typeof(String), "starts-with", typeof(StringFilter))]

    [InlineData(typeof(Enum), "equals", typeof(EnumFilter))]
    [InlineData(typeof(Enum), "not-equals", typeof(EnumFilter))]

    [InlineData(typeof(Guid), "equals", typeof(GuidFilter))]
    [InlineData(typeof(Guid), "not-equals", typeof(GuidFilter))]
    public void GridFilters_RegistersDefaultFilters(Type type, String method, Type filter)
    {
        Assert.IsType(filter, new GridFilters().Create(type, method, ""));
    }

    [Theory]
    [InlineData(typeof(Object))]
    [InlineData(typeof(Object[]))]
    public void Create_NotFoundForType_ReturnsNull(Type type)
    {
        Assert.Null(filters.Create(type, "equals", ""));
    }

    [Fact]
    public void Create_NotFoundFilterType_ReturnsNull()
    {
        Assert.Null(filters.Create(typeof(String), "less-than", ""));
    }

    [Fact]
    public void Create_NotFilterType_ReturnsNull()
    {
        filters.Register(typeof(String), "less-than", typeof(Object));

        Assert.Null(filters.Create(typeof(String), "less-than", ""));
    }

    [Fact]
    public void Create_ForNullableType()
    {
        IGridFilter? actual = filters.Create(typeof(Int32?), "EQUALS", "");

        Assert.Equal("equals", Assert.IsType<NumberFilter<Int32>>(actual).Method);
    }

    [Theory]
    [InlineData(typeof(TestEnum))]
    [InlineData(typeof(TestEnum?))]
    public void Create_ForSpecificEnumType(Type type)
    {
        filters.Register(type, "equals", typeof(StringFilter));

        IGridFilter? actual = filters.Create(type, "EQUALS", "");

        Assert.Equal("equals", Assert.IsType<StringFilter>(actual).Method);
    }

    [Theory]
    [InlineData(typeof(TestEnum))]
    [InlineData(typeof(TestEnum?))]
    public void Create_ForEnumType(Type type)
    {
        IGridFilter? actual = filters.Create(type, "EQUALS", "");

        Assert.Equal("equals", Assert.IsType<EnumFilter>(actual).Method);
    }

    [Fact]
    public void Create_ForType()
    {
        IGridFilter? actual = filters.Create(typeof(String), "CONTAINS", "");

        Assert.Equal("contains", Assert.IsType<StringFilter>(actual).Method);
    }

    [Theory]
    [InlineData(typeof(String[]))]
    [InlineData(typeof(List<String>))]
    [InlineData(typeof(IEnumerable<String>))]
    public void Create_ForEnumerableType(Type type)
    {
        IGridFilter? actual = filters.Create(type, "CONTAINS", "");

        Assert.Equal("contains", Assert.IsType<EnumerableFilter<StringFilter>>(actual).Method);
    }

    [Fact]
    public void Create_ForEnumerableEnumType()
    {
        IGridFilter? actual = filters.Create(typeof(IEnumerable<TestEnum>), "Equals", "");

        Assert.Equal("equals", Assert.IsType<EnumerableFilter<EnumFilter>>(actual).Method);
    }

    [Fact]
    public void Create_ForNotEnumerableGenericType()
    {
        filters.Register(typeof(GridModel), "equals", typeof(StringFilter));

        Assert.Null(filters.Create(typeof(GridColumn<GridModel, String>), "Equals", ""));
    }

    [Fact]
    public void OptionsFor_ForBoolean()
    {
        SelectListItem[] actual = filters.OptionsFor(Fake.Of<IGridColumn<GridModel, Boolean>>()).ToArray();

        Assert.Equal(3, actual.Length);
        Assert.Equal("", actual[0].Value);
        Assert.Equal("true", actual[1].Value);
        Assert.Equal("false", actual[2].Value);
        Assert.Equal(filters.BooleanTrueOptionText(), actual[1].Text);
        Assert.Equal(filters.BooleanEmptyOptionText(), actual[0].Text);
        Assert.Equal(filters.BooleanFalseOptionText(), actual[2].Text);
    }

    [Fact]
    public void OptionsFor_ForNullableBoolean()
    {
        SelectListItem[] actual = filters.OptionsFor(Fake.Of<IGridColumn<GridModel, Boolean?>>()).ToArray();

        Assert.Equal(3, actual.Length);
        Assert.Equal("", actual[0].Value);
        Assert.Equal("true", actual[1].Value);
        Assert.Equal("false", actual[2].Value);
        Assert.Equal(filters.BooleanTrueOptionText(), actual[1].Text);
        Assert.Equal(filters.BooleanEmptyOptionText(), actual[0].Text);
        Assert.Equal(filters.BooleanFalseOptionText(), actual[2].Text);
    }

    [Fact]
    public void OptionsFor_ForEnumerableBoolean()
    {
        SelectListItem[] actual = filters.OptionsFor(Fake.Of<IGridColumn<GridModel, Boolean?[]>>()).ToArray();

        Assert.Equal(3, actual.Length);
        Assert.Equal("", actual[0].Value);
        Assert.Equal("true", actual[1].Value);
        Assert.Equal("false", actual[2].Value);
        Assert.Equal(filters.BooleanTrueOptionText(), actual[1].Text);
        Assert.Equal(filters.BooleanEmptyOptionText(), actual[0].Text);
        Assert.Equal(filters.BooleanFalseOptionText(), actual[2].Text);
    }

    [Fact]
    public void OptionsFor_NullHttpContext_ForEnum()
    {
        GridColumn<GridModel, TestEnum> enumColumn = new(column.Grid, _ => TestEnum.First);

        SelectListItem[] actual = filters.OptionsFor(enumColumn).ToArray();

        Assert.Single(actual);
        Assert.Null(actual[0].Text);
        Assert.Null(actual[0].Value);
    }

    [Fact]
    public void OptionsFor_ForEnum()
    {
        IHtmlHelper helper = Fake.Of<IHtmlHelper>();
        GridColumn<GridModel, TestEnum> enumColumn = new(column.Grid, _ => TestEnum.First);
        column.Grid.HttpContext = Fake.Of<HttpContext>();
        Mock.Get(column.Grid.HttpContext).Setup(mock => mock.RequestServices.GetService(typeof(IHtmlHelper))).Returns(helper);
        Mock.Get(helper).Setup(mock => mock.GetEnumSelectList(typeof(TestEnum))).Returns(new[] { new SelectListItem { Value = "0", Text = "1st" }, new SelectListItem { Value = "1", Text = "2nd" } });

        SelectListItem[] actual = filters.OptionsFor(enumColumn).ToArray();

        Assert.Equal(3, actual.Length);

        Assert.Null(actual[0].Text);
        Assert.Null(actual[0].Value);
        Assert.Equal("0", actual[1].Value);
        Assert.Equal("1", actual[2].Value);
        Assert.Equal("1st", actual[1].Text);
        Assert.Equal("2nd", actual[2].Text);
    }

    [Fact]
    public void OptionsFor_ForNullableEnum()
    {
        IHtmlHelper helper = Fake.Of<IHtmlHelper>();
        GridColumn<GridModel, TestEnum?> enumColumn = new(column.Grid, _ => TestEnum.First);
        column.Grid.HttpContext = Fake.Of<HttpContext>();
        Mock.Get(column.Grid.HttpContext).Setup(mock => mock.RequestServices.GetService(typeof(IHtmlHelper))).Returns(helper);
        Mock.Get(helper).Setup(mock => mock.GetEnumSelectList(typeof(TestEnum))).Returns(new[] { new SelectListItem { Value = "0", Text = "1st" }, new SelectListItem { Value = "1", Text = "2nd" } });

        SelectListItem[] actual = filters.OptionsFor(enumColumn).ToArray();

        Assert.Equal(3, actual.Length);

        Assert.Null(actual[0].Text);
        Assert.Null(actual[0].Value);
        Assert.Equal("0", actual[1].Value);
        Assert.Equal("1", actual[2].Value);
        Assert.Equal("1st", actual[1].Text);
        Assert.Equal("2nd", actual[2].Text);
    }

    [Fact]
    public void OptionsFor_ForEnumerableEnum()
    {
        IHtmlHelper helper = Fake.Of<IHtmlHelper>();
        column.Grid.HttpContext = Fake.Of<HttpContext>();
        Mock.Get(column.Grid.HttpContext).Setup(mock => mock.RequestServices.GetService(typeof(IHtmlHelper))).Returns(helper);
        GridColumn<GridModel, IEnumerable<TestEnum?>> enumColumn = new(column.Grid, _ => new TestEnum?[] { TestEnum.First });
        Mock.Get(helper).Setup(mock => mock.GetEnumSelectList(typeof(TestEnum))).Returns(new[] { new SelectListItem { Value = "0", Text = "1st" }, new SelectListItem { Value = "1", Text = "2nd" } });

        SelectListItem[] actual = filters.OptionsFor(enumColumn).ToArray();

        Assert.Equal(3, actual.Length);

        Assert.Null(actual[0].Text);
        Assert.Null(actual[0].Value);
        Assert.Equal("0", actual[1].Value);
        Assert.Equal("1", actual[2].Value);
        Assert.Equal("1st", actual[1].Text);
        Assert.Equal("2nd", actual[2].Text);
    }

    [Fact]
    public void OptionsFor_ForOtherTypes()
    {
        Assert.Empty(filters.OptionsFor(Fake.Of<IGridColumn<GridModel, String>>()));
    }

    [Fact]
    public void Register_FilterForExistingType()
    {
        filters.Register(typeof(Int32), "TEST", typeof(Object));
        filters.Register(typeof(Int32), "TEST-FILTER", typeof(StringFilter));

        Assert.IsType<StringFilter>(filters.Create(typeof(Int32), "test-filter", ""));
    }

    [Fact]
    public void Register_NullableFilterTypeForExistingType()
    {
        filters.Register(typeof(Int32), "TEST", typeof(Object));
        filters.Register(typeof(Int32?), "TEST-FILTER", typeof(StringFilter));

        Assert.IsType<StringFilter>(filters.Create(typeof(Int32), "test-filter", ""));
    }

    [Fact]
    public void Register_Overrides_NullableFilter()
    {
        filters.Register(typeof(Int32), "test-filter", typeof(Object));
        filters.Register(typeof(Int32?), "TEST-filter", typeof(NumberFilter<Int32>));

        Assert.IsType<NumberFilter<Int32>>(filters.Create(typeof(Int32), "test-filter", ""));
    }

    [Fact]
    public void Register_Overrides_Filter()
    {
        filters.Register(typeof(Int32), "test-filter", typeof(Object));
        filters.Register(typeof(Int32), "TEST-filter", typeof(NumberFilter<Int32>));

        Assert.IsType<NumberFilter<Int32>>(filters.Create(typeof(Int32), "test-filter", ""));
    }

    [Fact]
    public void Register_NullableTypeAsNotNullable()
    {
        filters.Register(typeof(Int32?), "TEST", typeof(NumberFilter<Int32>));

        Assert.IsType<NumberFilter<Int32>>(filters.Create(typeof(Int32), "test", ""));
    }

    [Fact]
    public void Register_FilterForNewType()
    {
        filters.Register(typeof(Object), "test", typeof(NumberFilter<Int32>));

        Assert.IsType<NumberFilter<Int32>>(filters.Create(typeof(Object), "test", ""));
    }

    [Fact]
    public void Unregister_ExistingFilter()
    {
        filters.Register(typeof(Object), "test", typeof(StringFilter));

        filters.Unregister(typeof(Object), "TEST");

        Assert.Null(filters.Create(typeof(Object), "test", ""));
    }

    [Fact]
    public void Unregister_NotExistingFilter()
    {
        filters.Unregister(GetType(), "test");

        Assert.Null(filters.Create(GetType(), "test", ""));
    }

    [Fact]
    public void Unregister_NotExistingMethod()
    {
        filters.Register(typeof(Object), "test", typeof(StringFilter));

        filters.Unregister(typeof(Object), "method");

        Assert.Null(filters.Create(typeof(Object), "method", Array.Empty<String>()));
    }
}
