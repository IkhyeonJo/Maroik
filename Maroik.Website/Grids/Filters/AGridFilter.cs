using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Interfaces;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Grids.Filters;

public abstract class AGridFilter : IGridFilter
{
    public String? Method { get; set; }
    public StringValues Values { get; set; }
    public GridFilterCase Case { get; set; }

    public virtual Expression? Apply(Expression expression, CultureInfo culture)
    {
        Expression? filter = null;

        foreach (String? value in Values)
            if (Apply(expression, value, culture) is Expression next)
                filter = filter == null
                    ? next
                    : Method == "not-equals"
                        ? Expression.AndAlso(filter, next)
                        : Expression.OrElse(filter, next);

        return filter;
    }
    protected abstract Expression? Apply(Expression expression, String? value, CultureInfo culture);
}
