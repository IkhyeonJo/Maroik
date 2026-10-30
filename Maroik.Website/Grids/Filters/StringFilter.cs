using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace Maroik.Website.Grids.Filters;

public class StringFilter : AGridFilter
{
    protected static Expression Null { get; }
    protected static Expression Empty { get; }
    protected static MethodInfo ToLower { get; }
    protected static MethodInfo ToUpper { get; }
    protected static MethodInfo Contains { get; }
    protected static MethodInfo EndsWith { get; }
    protected static MethodInfo StartsWith { get; }

    static StringFilter()
    {
        Empty = Expression.Constant("");
        Null = Expression.Constant(null, typeof(String));
        ToLower = typeof(String).GetMethod(nameof(String.ToLower), Array.Empty<Type>())!;
        ToUpper = typeof(String).GetMethod(nameof(String.ToUpper), Array.Empty<Type>())!;
        Contains = typeof(String).GetMethod(nameof(String.Contains), new[] { typeof(String) })!;
        EndsWith = typeof(String).GetMethod(nameof(String.EndsWith), new[] { typeof(String) })!;
        StartsWith = typeof(String).GetMethod(nameof(String.StartsWith), new[] { typeof(String) })!;
    }

    public override Expression? Apply(Expression expression, CultureInfo culture)
    {
        if (Values.Count == 0)
            return null;

        switch (Method)
        {
            case "starts-with":
            case "ends-with":
            case "contains":
                if (Values.Any(String.IsNullOrEmpty))
                    return null;

                return Expression.AndAlso(Expression.NotEqual(expression, Null), base.Apply(expression, culture)!);
            case "consists-of":
                if (Values.All(String.IsNullOrWhiteSpace))
                    return null;

                return Expression.AndAlso(Expression.NotEqual(expression, Null), base.Apply(expression, culture)!);
            case "not-equals":
                if (Case == GridFilterCase.Original)
                    return base.Apply(expression, culture);

                if (Values.Any(String.IsNullOrEmpty))
                    return Expression.AndAlso(Apply(expression, null, culture)!, base.Apply(expression, culture)!);

                return Expression.OrElse(Expression.Equal(expression, Null), base.Apply(expression, culture)!);
            case "equals":
                if (Case == GridFilterCase.Original)
                    return base.Apply(expression, culture);

                if (Values.Any(String.IsNullOrEmpty))
                    return Expression.OrElse(Apply(expression, null, culture)!, base.Apply(expression, culture)!);

                return Expression.AndAlso(Expression.NotEqual(expression, Null), base.Apply(expression, culture)!);
        }

        return base.Apply(expression, culture);
    }

    protected override Expression? Apply(Expression expression, String? value, CultureInfo culture)
    {
        return Method switch
        {
            "not-equals" => String.IsNullOrEmpty(value)
                ? Expression.AndAlso(Expression.NotEqual(expression, Null), Expression.NotEqual(expression, Empty))
                : Expression.NotEqual(ConvertCase(expression), ConvertCase(value)),
            "equals" => String.IsNullOrEmpty(value)
                ? Expression.OrElse(Expression.Equal(expression, Null), Expression.Equal(expression, Empty))
                : Expression.Equal(ConvertCase(expression), ConvertCase(value)),
            "starts-with" => Expression.Call(ConvertCase(expression), StartsWith, ConvertCase(value!)),
            "ends-with" => Expression.Call(ConvertCase(expression), EndsWith, ConvertCase(value!)),
            "contains" => Expression.Call(ConvertCase(expression), Contains, ConvertCase(value!)),
            "consists-of" => String.IsNullOrWhiteSpace(value) ? null : Consists(expression, value),
            _ => null
        };
    }
    protected Expression? Consists(Expression expression, String value)
    {
        Expression? filter = null;

        foreach (String val in value.Split(' ').Distinct())
        {
            if (String.IsNullOrWhiteSpace(val))
                continue;

            if (filter == null)
                filter = Expression.Call(ConvertCase(expression), Contains, ConvertCase(val));
            else
                filter = Expression.AndAlso(filter, Expression.Call(ConvertCase(expression), Contains, ConvertCase(val)));
        }

        return filter;
    }
    protected Expression ConvertCase(Expression expression)
    {
        return Case switch
        {
            GridFilterCase.Upper => Expression.Call(expression, ToUpper),
            GridFilterCase.Lower => Expression.Call(expression, ToLower),
            _ => expression
        };
    }
    protected Expression ConvertCase(String value)
    {
        return Expression.Constant(Case switch
        {
            GridFilterCase.Upper => value.ToUpper(CultureInfo.CurrentCulture),
            GridFilterCase.Lower => value.ToLower(CultureInfo.CurrentCulture),
            _ => value
        });
    }
}
