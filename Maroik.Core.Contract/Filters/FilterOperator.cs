namespace Maroik.Core.Contract.Filters;

/// <summary>
/// Comparison operators used to construct a <see cref="FilterCondition"/>.
/// The repository implementation translates each member to the corresponding SQL predicate.
/// </summary>
public enum FilterOperator
{
    /// <summary>Exact equality: <c>col = value</c>.</summary>
    Equal,

    /// <summary>Inequality: <c>col != value</c>.</summary>
    NotEqual,

    /// <summary>Strictly greater: <c>col &gt; value</c>.</summary>
    GreaterThan,

    /// <summary>Greater than or equal: <c>col &gt;= value</c>.</summary>
    GreaterThanOrEqual,

    /// <summary>Strictly less: <c>col &lt; value</c>.</summary>
    LessThan,

    /// <summary>Less than or equal: <c>col &lt;= value</c>.</summary>
    LessThanOrEqual,

    /// <summary>Case-sensitive substring match: <c>col LIKE '%value%'</c>.</summary>
    Contains,

    /// <summary>Prefix match: <c>col LIKE 'value%'</c>.</summary>
    StartsWith,

    /// <summary>Suffix match: <c>col LIKE '%value'</c>.</summary>
    EndsWith,

    /// <summary>PostgreSQL case-insensitive pattern match (ILIKE). Use % and _ as wildcards.</summary>
    Like,

    /// <summary>Set membership: <c>col IN (values)</c>.</summary>
    In,

    /// <summary>Null check: <c>col IS NULL</c>.</summary>
    IsNull,

    /// <summary>Not-null check: <c>col IS NOT NULL</c>.</summary>
    IsNotNull
}
