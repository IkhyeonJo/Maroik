using System.ComponentModel.DataAnnotations;
using Maroik.Website.Grids.Columns;
using Microsoft.AspNetCore.Html;

namespace Maroik.Website.Tests.Grids.Fixtures;

public class GridModel
{
    [Display(Name = "Text")]
    public String? Text { get; set; }

    [Display(Name = "Text", ShortName = "Txt")]
    public String? ShortText { get; set; }

    public IHtmlContent? Content { get; set; }
    public Boolean? NIsChecked { get; set; }
    public DateOnly? NDateOnly { get; set; }
    public TimeOnly? NTimeOnly { get; set; }
    public Boolean IsChecked { get; set; }
    public DateOnly DateOnly { get; set; }
    public TimeOnly TimeOnly { get; set; }
    public DateTime? NDate { get; set; }
    public TestEnum? NEnum { get; set; }
    public DateTime Date { get; set; }
    public TestEnum Enum { get; set; }
    public String? Name { get; set; }
    public Int32? NSum { get; set; }
    public Guid? NGuid { get; set; }
    public Int32 Sum { get; set; }
    public Guid Guid { get; set; }

    public TestEnum EnumField { get; set; }
    public Guid GuidField { get; set; }
    public SByte SByteField { get; set; }
    public Byte ByteField { get; set; }
    public Int16 Int16Field { get; set; }
    public UInt16 UInt16Field { get; set; }
    public Int32 Int32Field { get; set; }
    public UInt32 UInt32Field { get; set; }
    public Int64 Int64Field { get; set; }
    public UInt64 UInt64Field { get; set; }
    public Single SingleField { get; set; }
    public Double DoubleField { get; set; }
    public Decimal DecimalField { get; set; }
    public Boolean BooleanField { get; set; }
    public DateOnly DateOnlyField { get; set; }
    public DateTime DateTimeField { get; set; }
    public TimeOnly TimeOnlyField { get; set; }

    public TestEnum? NullableEnumField { get; set; }
    public Guid? NullableGuidField { get; set; }
    public SByte? NullableSByteField { get; set; }
    public Byte? NullableByteField { get; set; }
    public Int16? NullableInt16Field { get; set; }
    public UInt16? NullableUInt16Field { get; set; }
    public Int32? NullableInt32Field { get; set; }
    public UInt32? NullableUInt32Field { get; set; }
    public Int64? NullableInt64Field { get; set; }
    public UInt64? NullableUInt64Field { get; set; }
    public Single? NullableSingleField { get; set; }
    public Double? NullableDoubleField { get; set; }
    public Decimal? NullableDecimalField { get; set; }
    public Boolean? NullableBooleanField { get; set; }
    public DateOnly? NullableDateOnlyField { get; set; }
    public DateTime? NullableDateTimeField { get; set; }
    public TimeOnly? NullableTimeOnlyField { get; set; }

    public String?[]? NullableArrayField { get; set; }
    public List<String?>? NullableListField { get; set; }
    public GridColumn<String, Int32>? GridColumnField { get; set; }
    public IEnumerable<String?>? NullableEnumerableField { get; set; }

    public String? StringField { get; set; }

    public GridModel? Child { get; set; }
}
