using System.Linq.Expressions;
using Maroik.Website.Grids.Interfaces;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Grids.Filters;

public class GridColumnFilter<T, TValue> : IGridColumnFilter<T, TValue>
{
    public String Name { get; set; }
    public Boolean? IsEnabled { get; set; }
    public String DefaultMethod { get; set; }
    public GridFilterCase? Case { get; set; }
    public GridFilterType? Type { get; set; }

    private Boolean OptionsIsSet { get; set; }
    public virtual IEnumerable<SelectListItem> Options
    {
        get
        {
            if (IsEnabled == true && !OptionsIsSet)
                Options = GetFilters().OptionsFor(Column);

            return OptionsValue;
        }
        set
        {
            OptionsValue = value;
            OptionsIsSet = true;
        }
    }
    private IEnumerable<SelectListItem> OptionsValue { get; set; }

    public virtual String? Operator
    {
        get
        {
            if (IsEnabled == true && (Type == GridFilterType.Double || Type == GridFilterType.Auto) && !OperatorIsSet)
            {
                String prefix = String.IsNullOrEmpty(Column.Grid.Name) ? "" : Column.Grid.Name + "-";
                Operator = Column.Grid.Query?[prefix + Column.Name + "-op"].FirstOrDefault()?.ToLowerInvariant();
            }

            return OperatorValue;
        }
        set
        {
            OperatorValue = value;
            OperatorIsSet = true;
        }
    }
    private String? OperatorValue { get; set; }
    private Boolean OperatorIsSet { get; set; }

    public virtual IGridFilter? First
    {
        get
        {
            if (IsEnabled == true && !FirstIsSet)
                First = CreateFirstFilter();

            return FirstValue;
        }
        set
        {
            FirstValue = value;
            FirstIsSet = true;
        }
    }
    private Boolean FirstIsSet { get; set; }
    private IGridFilter? FirstValue { get; set; }

    public virtual IGridFilter? Second
    {
        get
        {
            if (IsEnabled == true && !SecondIsSet && Operator?.Length > 0)
                if (Type == GridFilterType.Auto || Type == GridFilterType.Double)
                    Second = CreateSecondFilter();

            return SecondValue;
        }
        set
        {
            SecondValue = value;
            SecondIsSet = true;
        }
    }
    private Boolean SecondIsSet { get; set; }
    private IGridFilter? SecondValue { get; set; }

    public IGridColumn<T, TValue> Column { get; set; }

    public GridColumnFilter(IGridColumn<T, TValue> column)
    {
        Column = column;
        Name = GetName();
        DefaultMethod = "";
        OptionsValue = Array.Empty<SelectListItem>();
        IsEnabled = column.Expression.Body is MemberExpression ? IsEnabled : false;
    }

    public IQueryable<T> Apply(IQueryable<T> items)
    {
        if (IsEnabled != true)
            return items;

        Expression? expression = BuildFilterExpression();

        return expression == null ? items : items.Where(ToLambda(expression));
    }

    private IGridFilters GetFilters()
    {
        return Column.Grid.HttpContext?.RequestServices.GetService<IGridFilters>() ?? new GridFilters();
    }
    private IGridFilter? CreateFirstFilter()
    {
        String prefix = String.IsNullOrEmpty(Column.Grid.Name) ? "" : Column.Grid.Name + "-";
        String columnName = (prefix + Column.Name + "-").ToLowerInvariant();
        String[] keys = FilterKeysFor(columnName);

        if (keys.Length == 0)
            return null;

        String method = keys[0][columnName.Length..];

        if (Type == GridFilterType.Multi || Type == GridFilterType.Auto && String.IsNullOrEmpty(Operator))
            return CreateFilter(method, Column.Grid.Query![keys[0]]);

        return CreateFilter(method, Column.Grid.Query![keys[0]][0]);
    }
    private IGridFilter? CreateSecondFilter()
    {
        String prefix = String.IsNullOrEmpty(Column.Grid.Name) ? "" : Column.Grid.Name + "-";
        String columnName = (prefix + Column.Name + "-").ToLowerInvariant();
        String[] keys = FilterKeysFor(columnName);

        if (keys.Length == 0)
            return null;

        if (keys.Length == 1)
        {
            StringValues values = Column.Grid.Query![keys[0]];

            if (values.Count < 2)
                return null;

            return CreateFilter(keys[0][columnName.Length..], values[1]);
        }

        String method = keys[1][columnName.Length..];
        String? value = Column.Grid.Query![keys[1]][0];

        return CreateFilter(method, value);
    }
    private IGridFilter? CreateFilter(String method, StringValues values)
    {
        if (GetFilters().Create(typeof(TValue), method, values) is not IGridFilter filter)
            return null;

        filter.Case = Case ?? GridFilterCase.Original;

        return filter;
    }

    private String GetName()
    {
        Type type = GetFilterable(typeof(TValue));
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return "default";

        switch (System.Type.GetTypeCode(type))
        {
            case TypeCode.SByte:
            case TypeCode.Byte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                return "number";
            case TypeCode.String:
                return "text";
            case TypeCode.DateTime:
                return "date";
            default:
                if (type == typeof(DateOnly))
                    return "date-only";

                if (type == typeof(TimeOnly))
                    return "time-only";

                return type == typeof(Guid) ? "guid" : "default";
        }
    }
    private Type GetFilterable(Type type)
    {
        if (type == typeof(String))
            return type;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            return type.GetGenericArguments()[0];

        foreach (Type interfaceType in type.GetInterfaces())
            if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return interfaceType.GetGenericArguments()[0];

        return type;
    }
    private Expression? BuildFilterExpression()
    {
        Expression? left = First?.Apply(Column.Expression.Body, Column.Grid.Culture);

        if (left != null && (Type == GridFilterType.Double || Type == GridFilterType.Auto))
        {
            Expression? right = Second?.Apply(Column.Expression.Body, Column.Grid.Culture);

            if (right != null && "and".Equals(Operator, StringComparison.OrdinalIgnoreCase))
                return Expression.AndAlso(left, right);

            if (right != null && "or".Equals(Operator, StringComparison.OrdinalIgnoreCase))
                return Expression.OrElse(left, right);
        }

        return left;
    }
    private String[] FilterKeysFor(String prefix)
    {
        return Column
            .Grid
            .Query?
            .Keys
            .Where(key =>
                key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                !key.Equals(prefix + "op", StringComparison.OrdinalIgnoreCase))
            .ToArray() ?? Array.Empty<String>();
    }
    private Expression<Func<T, Boolean>> ToLambda(Expression expression)
    {
        return Expression.Lambda<Func<T, Boolean>>(expression, Column.Expression.Parameters[0]);
    }
}
