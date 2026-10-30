using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;

namespace Maroik.Website.Grids.Filters;

public class DateTimeFilter : AGridFilter
{
    protected override Expression? Apply(Expression expression, String? value, CultureInfo culture)
    {
        if (String.IsNullOrEmpty(value) && Nullable.GetUnderlyingType(expression.Type) == null)
            expression = Expression.Convert(expression, typeof(Nullable<>).MakeGenericType(expression.Type));

        try
        {
            Object dateValue = TypeDescriptor.GetConverter(expression.Type).ConvertFrom(null, culture, value!)!;

            return Method switch
            {
                "later-than-or-equal" => Expression.GreaterThanOrEqual(expression, Expression.Constant(dateValue, expression.Type)),
                "earlier-than-or-equal" => Expression.LessThanOrEqual(expression, Expression.Constant(dateValue, expression.Type)),
                "later-than" => Expression.GreaterThan(expression, Expression.Constant(dateValue, expression.Type)),
                "earlier-than" => Expression.LessThan(expression, Expression.Constant(dateValue, expression.Type)),
                "not-equals" => Expression.NotEqual(expression, Expression.Constant(dateValue, expression.Type)),
                "equals" => Expression.Equal(expression, Expression.Constant(dateValue, expression.Type)),
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
