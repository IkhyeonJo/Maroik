using System.Globalization;
using System.Linq.Expressions;
using Maroik.Website.Grids.Filters;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridFilter
{
    String? Method { get; set; }
    StringValues Values { get; set; }
    GridFilterCase Case { get; set; }

    Expression? Apply(Expression expression, CultureInfo culture);
}
