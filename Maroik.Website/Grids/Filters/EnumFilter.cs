using System.ComponentModel;
using System.Globalization;
using System.Linq.Expressions;

namespace Maroik.Website.Grids.Filters;

public class EnumFilter : AGridFilter
{
    protected override Expression? Apply(Expression expression, String? value, CultureInfo culture)
    {
        if (String.IsNullOrEmpty(value) && Nullable.GetUnderlyingType(expression.Type) == null)
            expression = Expression.Convert(expression, typeof(Nullable<>).MakeGenericType(expression.Type));

        try
        {
            Object enumValue = TypeDescriptor.GetConverter(expression.Type).ConvertFrom(null, culture, value!)!;

            return Method switch
            {
                "not-equals" => Expression.NotEqual(expression, Expression.Constant(enumValue, expression.Type)),
                "equals" => Expression.Equal(expression, Expression.Constant(enumValue, expression.Type)),
                _ => null
            };
        }
        // An expected condition, not a fault: the filter value comes from the query string, and one the column's type cannot
        // take means "no filter", as upstream NonFactors 8.0.0 does. Narrowed from upstream's bare catch to what the type
        // converters throw for such a value: FormatException (bool / date / enum / guid text), ArgumentException (the number
        // converters wrap both unparsable and out-of-range text in it).
        catch (Exception ex) when (ex is FormatException or ArgumentException)
        {
            return null;
        }
    }
}
