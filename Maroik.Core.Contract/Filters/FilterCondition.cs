using System.Collections;

namespace Maroik.Core.Contract.Filters;

public sealed record FilterCondition(string PropertyName, FilterOperator Operator, object? Value = null)
{
    public static FilterCondition Equal(string property, object value)
        => new(property, FilterOperator.Equal, value);

    public static FilterCondition NotEqual(string property, object value)
        => new(property, FilterOperator.NotEqual, value);

    public static FilterCondition GreaterThan(string property, object value)
        => new(property, FilterOperator.GreaterThan, value);

    public static FilterCondition GreaterThanOrEqual(string property, object value)
        => new(property, FilterOperator.GreaterThanOrEqual, value);

    public static FilterCondition LessThan(string property, object value)
        => new(property, FilterOperator.LessThan, value);

    public static FilterCondition LessThanOrEqual(string property, object value)
        => new(property, FilterOperator.LessThanOrEqual, value);

    public static FilterCondition Contains(string property, string value)
        => new(property, FilterOperator.Contains, value);

    public static FilterCondition StartsWith(string property, string value)
        => new(property, FilterOperator.StartsWith, value);

    public static FilterCondition EndsWith(string property, string value)
        => new(property, FilterOperator.EndsWith, value);

    /// <summary>PostgreSQL ILIKE – case-insensitive pattern match. Use % and _ as wildcards.</summary>
    public static FilterCondition Like(string property, string pattern)
        => new(property, FilterOperator.Like, pattern);

    public static FilterCondition In(string property, IEnumerable values)
        => new(property, FilterOperator.In, values);

    public static FilterCondition IsNull(string property)
        => new(property, FilterOperator.IsNull);

    public static FilterCondition IsNotNull(string property)
        => new(property, FilterOperator.IsNotNull);
}
