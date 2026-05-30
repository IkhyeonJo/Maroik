using System.Collections;

namespace Maroik.Core.Contract.Filters;

/// <summary>
/// Represents a single filter predicate passed to <see cref="Maroik.Core.Contract.Interfaces.IGenericRepository{TDomain}.FindAsync"/>.
/// Combines a property name, an operator, and an optional comparison value.
/// Use the static factory methods to construct type-safe instances.
/// </summary>
public sealed record FilterCondition(string PropertyName, FilterOperator Operator, object? Value = null)
{
    /// <summary>Creates an equality filter: <c>PropertyName = Value</c>.</summary>
    public static FilterCondition Equal(string property, object value)
        => new(property, FilterOperator.Equal, value);

    /// <summary>Creates a not-equal filter: <c>PropertyName != Value</c>.</summary>
    public static FilterCondition NotEqual(string property, object value)
        => new(property, FilterOperator.NotEqual, value);

    /// <summary>Creates a greater-than filter: <c>PropertyName > Value</c>.</summary>
    public static FilterCondition GreaterThan(string property, object value)
        => new(property, FilterOperator.GreaterThan, value);

    /// <summary>Creates a greater-than-or-equal filter: <c>PropertyName >= Value</c>.</summary>
    public static FilterCondition GreaterThanOrEqual(string property, object value)
        => new(property, FilterOperator.GreaterThanOrEqual, value);

    /// <summary>Creates a less-than filter: <c>PropertyName &lt; Value</c>.</summary>
    public static FilterCondition LessThan(string property, object value)
        => new(property, FilterOperator.LessThan, value);

    /// <summary>Creates a less-than-or-equal filter: <c>PropertyName &lt;= Value</c>.</summary>
    public static FilterCondition LessThanOrEqual(string property, object value)
        => new(property, FilterOperator.LessThanOrEqual, value);

    /// <summary>Creates a case-sensitive substring filter: <c>PropertyName LIKE '%Value%'</c>.</summary>
    public static FilterCondition Contains(string property, string value)
        => new(property, FilterOperator.Contains, value);

    /// <summary>Creates a prefix filter: <c>PropertyName LIKE 'Value%'</c>.</summary>
    public static FilterCondition StartsWith(string property, string value)
        => new(property, FilterOperator.StartsWith, value);

    /// <summary>Creates a suffix filter: <c>PropertyName LIKE '%Value'</c>.</summary>
    public static FilterCondition EndsWith(string property, string value)
        => new(property, FilterOperator.EndsWith, value);

    /// <summary>PostgreSQL ILIKE – case-insensitive pattern match. Use % and _ as wildcards.</summary>
    public static FilterCondition Like(string property, string pattern)
        => new(property, FilterOperator.Like, pattern);

    /// <summary>Creates an IN filter: <c>PropertyName IN (Values)</c>.</summary>
    public static FilterCondition In(string property, IEnumerable values)
        => new(property, FilterOperator.In, values);

    /// <summary>Creates a null check filter: <c>PropertyName IS NULL</c>.</summary>
    public static FilterCondition IsNull(string property)
        => new(property, FilterOperator.IsNull);

    /// <summary>Creates a not-null check filter: <c>PropertyName IS NOT NULL</c>.</summary>
    public static FilterCondition IsNotNull(string property)
        => new(property, FilterOperator.IsNotNull);
}
