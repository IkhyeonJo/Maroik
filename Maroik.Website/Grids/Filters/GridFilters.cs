using Maroik.Website.Grids.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Grids.Filters;

public class GridFilters : IGridFilters
{
    public Func<String> BooleanTrueOptionText { get; set; }
    public Func<String> BooleanFalseOptionText { get; set; }
    public Func<String> BooleanEmptyOptionText { get; set; }

    private Dictionary<Type, IDictionary<String, Type>> Filters
    {
        get;
    }

    public GridFilters()
    {
        BooleanEmptyOptionText = () => "";
        BooleanTrueOptionText = () => "True";
        BooleanFalseOptionText = () => "False";
        Filters = new Dictionary<Type, IDictionary<String, Type>>();

        Register(typeof(SByte), "equals", typeof(NumberFilter<SByte>));
        Register(typeof(SByte), "not-equals", typeof(NumberFilter<SByte>));
        Register(typeof(SByte), "less-than", typeof(NumberFilter<SByte>));
        Register(typeof(SByte), "greater-than", typeof(NumberFilter<SByte>));
        Register(typeof(SByte), "less-than-or-equal", typeof(NumberFilter<SByte>));
        Register(typeof(SByte), "greater-than-or-equal", typeof(NumberFilter<SByte>));

        Register(typeof(Byte), "equals", typeof(NumberFilter<Byte>));
        Register(typeof(Byte), "not-equals", typeof(NumberFilter<Byte>));
        Register(typeof(Byte), "less-than", typeof(NumberFilter<Byte>));
        Register(typeof(Byte), "greater-than", typeof(NumberFilter<Byte>));
        Register(typeof(Byte), "less-than-or-equal", typeof(NumberFilter<Byte>));
        Register(typeof(Byte), "greater-than-or-equal", typeof(NumberFilter<Byte>));

        Register(typeof(Int16), "equals", typeof(NumberFilter<Int16>));
        Register(typeof(Int16), "not-equals", typeof(NumberFilter<Int16>));
        Register(typeof(Int16), "less-than", typeof(NumberFilter<Int16>));
        Register(typeof(Int16), "greater-than", typeof(NumberFilter<Int16>));
        Register(typeof(Int16), "less-than-or-equal", typeof(NumberFilter<Int16>));
        Register(typeof(Int16), "greater-than-or-equal", typeof(NumberFilter<Int16>));

        Register(typeof(UInt16), "equals", typeof(NumberFilter<UInt16>));
        Register(typeof(UInt16), "not-equals", typeof(NumberFilter<UInt16>));
        Register(typeof(UInt16), "less-than", typeof(NumberFilter<UInt16>));
        Register(typeof(UInt16), "greater-than", typeof(NumberFilter<UInt16>));
        Register(typeof(UInt16), "less-than-or-equal", typeof(NumberFilter<UInt16>));
        Register(typeof(UInt16), "greater-than-or-equal", typeof(NumberFilter<UInt16>));

        Register(typeof(Int32), "equals", typeof(NumberFilter<Int32>));
        Register(typeof(Int32), "not-equals", typeof(NumberFilter<Int32>));
        Register(typeof(Int32), "less-than", typeof(NumberFilter<Int32>));
        Register(typeof(Int32), "greater-than", typeof(NumberFilter<Int32>));
        Register(typeof(Int32), "less-than-or-equal", typeof(NumberFilter<Int32>));
        Register(typeof(Int32), "greater-than-or-equal", typeof(NumberFilter<Int32>));

        Register(typeof(UInt32), "equals", typeof(NumberFilter<UInt32>));
        Register(typeof(UInt32), "not-equals", typeof(NumberFilter<UInt32>));
        Register(typeof(UInt32), "less-than", typeof(NumberFilter<UInt32>));
        Register(typeof(UInt32), "greater-than", typeof(NumberFilter<UInt32>));
        Register(typeof(UInt32), "less-than-or-equal", typeof(NumberFilter<UInt32>));
        Register(typeof(UInt32), "greater-than-or-equal", typeof(NumberFilter<UInt32>));

        Register(typeof(Int64), "equals", typeof(NumberFilter<Int64>));
        Register(typeof(Int64), "not-equals", typeof(NumberFilter<Int64>));
        Register(typeof(Int64), "less-than", typeof(NumberFilter<Int64>));
        Register(typeof(Int64), "greater-than", typeof(NumberFilter<Int64>));
        Register(typeof(Int64), "less-than-or-equal", typeof(NumberFilter<Int64>));
        Register(typeof(Int64), "greater-than-or-equal", typeof(NumberFilter<Int64>));

        Register(typeof(UInt64), "equals", typeof(NumberFilter<UInt64>));
        Register(typeof(UInt64), "not-equals", typeof(NumberFilter<UInt64>));
        Register(typeof(UInt64), "less-than", typeof(NumberFilter<UInt64>));
        Register(typeof(UInt64), "greater-than", typeof(NumberFilter<UInt64>));
        Register(typeof(UInt64), "less-than-or-equal", typeof(NumberFilter<UInt64>));
        Register(typeof(UInt64), "greater-than-or-equal", typeof(NumberFilter<UInt64>));

        Register(typeof(Single), "equals", typeof(NumberFilter<Single>));
        Register(typeof(Single), "not-equals", typeof(NumberFilter<Single>));
        Register(typeof(Single), "less-than", typeof(NumberFilter<Single>));
        Register(typeof(Single), "greater-than", typeof(NumberFilter<Single>));
        Register(typeof(Single), "less-than-or-equal", typeof(NumberFilter<Single>));
        Register(typeof(Single), "greater-than-or-equal", typeof(NumberFilter<Single>));

        Register(typeof(Double), "equals", typeof(NumberFilter<Double>));
        Register(typeof(Double), "not-equals", typeof(NumberFilter<Double>));
        Register(typeof(Double), "less-than", typeof(NumberFilter<Double>));
        Register(typeof(Double), "greater-than", typeof(NumberFilter<Double>));
        Register(typeof(Double), "less-than-or-equal", typeof(NumberFilter<Double>));
        Register(typeof(Double), "greater-than-or-equal", typeof(NumberFilter<Double>));

        Register(typeof(Decimal), "equals", typeof(NumberFilter<Decimal>));
        Register(typeof(Decimal), "not-equals", typeof(NumberFilter<Decimal>));
        Register(typeof(Decimal), "less-than", typeof(NumberFilter<Decimal>));
        Register(typeof(Decimal), "greater-than", typeof(NumberFilter<Decimal>));
        Register(typeof(Decimal), "less-than-or-equal", typeof(NumberFilter<Decimal>));
        Register(typeof(Decimal), "greater-than-or-equal", typeof(NumberFilter<Decimal>));

        Register(typeof(DateOnly), "equals", typeof(DateTimeFilter));
        Register(typeof(DateOnly), "not-equals", typeof(DateTimeFilter));
        Register(typeof(DateOnly), "earlier-than", typeof(DateTimeFilter));
        Register(typeof(DateOnly), "later-than", typeof(DateTimeFilter));
        Register(typeof(DateOnly), "earlier-than-or-equal", typeof(DateTimeFilter));
        Register(typeof(DateOnly), "later-than-or-equal", typeof(DateTimeFilter));

        Register(typeof(DateTime), "equals", typeof(DateTimeFilter));
        Register(typeof(DateTime), "not-equals", typeof(DateTimeFilter));
        Register(typeof(DateTime), "earlier-than", typeof(DateTimeFilter));
        Register(typeof(DateTime), "later-than", typeof(DateTimeFilter));
        Register(typeof(DateTime), "earlier-than-or-equal", typeof(DateTimeFilter));
        Register(typeof(DateTime), "later-than-or-equal", typeof(DateTimeFilter));

        Register(typeof(TimeOnly), "equals", typeof(DateTimeFilter));
        Register(typeof(TimeOnly), "not-equals", typeof(DateTimeFilter));
        Register(typeof(TimeOnly), "earlier-than", typeof(DateTimeFilter));
        Register(typeof(TimeOnly), "later-than", typeof(DateTimeFilter));
        Register(typeof(TimeOnly), "earlier-than-or-equal", typeof(DateTimeFilter));
        Register(typeof(TimeOnly), "later-than-or-equal", typeof(DateTimeFilter));

        Register(typeof(Boolean), "equals", typeof(BooleanFilter));
        Register(typeof(Boolean), "not-equals", typeof(BooleanFilter));

        Register(typeof(String), "equals", typeof(StringFilter));
        Register(typeof(String), "not-equals", typeof(StringFilter));
        Register(typeof(String), "contains", typeof(StringFilter));
        Register(typeof(String), "consists-of", typeof(StringFilter));
        Register(typeof(String), "ends-with", typeof(StringFilter));
        Register(typeof(String), "starts-with", typeof(StringFilter));

        Register(typeof(Enum), "equals", typeof(EnumFilter));
        Register(typeof(Enum), "not-equals", typeof(EnumFilter));

        Register(typeof(Guid), "equals", typeof(GuidFilter));
        Register(typeof(Guid), "not-equals", typeof(GuidFilter));
    }

    public virtual IGridFilter? Create(Type type, String method, StringValues values)
    {
        if (Get(Nullable.GetUnderlyingType(type) ?? type, method) is not Type filterType)
            return null;

        if (Activator.CreateInstance(filterType) is not IGridFilter filter)
            return null;

        filter.Method = method.ToLowerInvariant();
        filter.Values = values;

        return filter;
    }
    public virtual IEnumerable<SelectListItem> OptionsFor<T, TValue>(IGridColumn<T, TValue> column)
    {
        Type type = GetElementType(typeof(TValue)) ?? typeof(TValue);
        type = Nullable.GetUnderlyingType(type) ?? type;
        List<SelectListItem> options = new();

        if (type == typeof(Boolean))
        {
            options.Add(new SelectListItem { Value = "", Text = BooleanEmptyOptionText.Invoke() });
            options.Add(new SelectListItem { Value = "true", Text = BooleanTrueOptionText.Invoke() });
            options.Add(new SelectListItem { Value = "false", Text = BooleanFalseOptionText.Invoke() });
        }
        else if (type.IsEnum)
        {
            options.Add(new SelectListItem());

            IEnumerable<SelectListItem>? items = column.Grid
                .HttpContext?
                .RequestServices
                .GetRequiredService<IHtmlHelper>()
                .GetEnumSelectList(type)
                .OrderBy(item => item.Text);

            if (items != null)
                options.AddRange(items);
        }

        return options;
    }

    public void Register(Type type, String method, Type filter)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (!Filters.ContainsKey(type))
            Filters[type] = new Dictionary<String, Type>(StringComparer.OrdinalIgnoreCase);

        Filters[type][method] = filter;
    }
    public void Unregister(Type type, String method)
    {
        if (Filters.ContainsKey(type))
            Filters[type].Remove(method);
    }

    private Boolean TryGet(Type type, String method, out Type? filter)
    {
        if (Filters.ContainsKey(type) && Filters[type].ContainsKey(method))
            filter = Filters[type][method];
        else
            filter = null;

        return filter != null;
    }
    private Type? Get(Type type, String method)
    {
        if (TryGet(type, method, out Type? filter))
            return filter;

        if (GetElementType(type) is Type elementType)
        {
            if (TryGet(elementType, method, out filter))
                return typeof(EnumerableFilter<>).MakeGenericType(filter!);

            if (elementType.IsEnum && TryGet(typeof(Enum), method, out filter))
                return typeof(EnumerableFilter<>).MakeGenericType(filter!);
        }
        else if (type.IsEnum && TryGet(typeof(Enum), method, out filter))
        {
            return filter;
        }

        return null;
    }
    private Type? GetElementType(Type type)
    {
        if (type == typeof(String))
            return null;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        foreach (Type interfaceType in type.GetInterfaces())
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return interfaceType.GetGenericArguments()[0];

        return null;
    }
}
