using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;

namespace Maroik.Website.Grids.Filters;

public class NumberFilter<T> : AGridFilter
{
    protected override Expression? Apply(Expression expression, String? value, CultureInfo culture)
    {
        if (String.IsNullOrEmpty(value) && Nullable.GetUnderlyingType(expression.Type) == null)
            expression = Expression.Convert(expression, typeof(Nullable<>).MakeGenericType(expression.Type));

        try
        {
            Object? numberValue = String.IsNullOrEmpty(value) ? null : TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(null, culture, value);

            return Method switch
            {
                "greater-than-or-equal" => Expression.GreaterThanOrEqual(expression, Expression.Constant(numberValue, expression.Type)),
                "less-than-or-equal" => Expression.LessThanOrEqual(expression, Expression.Constant(numberValue, expression.Type)),
                "greater-than" => Expression.GreaterThan(expression, Expression.Constant(numberValue, expression.Type)),
                "not-equals" => Expression.NotEqual(expression, Expression.Constant(numberValue, expression.Type)),
                "less-than" => Expression.LessThan(expression, Expression.Constant(numberValue, expression.Type)),
                "equals" => Expression.Equal(expression, Expression.Constant(numberValue, expression.Type)),
                _ => null
            };
        }
        // An expected condition, not a fault: the filter value comes from the query string, and one the column's type cannot
        // take means "no filter", as the upstream grid 8.0.0 does (Grids/LICENSE.md). Narrowed from upstream's bare catch to what the type
        // converters throw for such a value: FormatException (bool / date / enum / guid text), ArgumentException (the number
        // converters wrap both unparsable and out-of-range text in it).
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return null;
        }
    }
}
