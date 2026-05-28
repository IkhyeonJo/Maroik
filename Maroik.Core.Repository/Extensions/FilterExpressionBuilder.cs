using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using Maroik.Core.Contract.Filters;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Maroik.Core.Repository.Extensions;

/// <summary>
/// Converts a list of <see cref="FilterCondition"/> records into an EF Core-translatable
/// <see cref="Expression{TDelegate}"/> predicate by building an Expression Tree at runtime.
/// All conditions are combined with AND.
/// The <see cref="FilterOperator.Like"/> operator uses PostgreSQL ILIKE (case-insensitive).
/// </summary>
public static class FilterExpressionBuilder
{
    // Resolved once and cached – avoids per-call reflection overhead.
    private static readonly MethodInfo? _iLikeMethod = typeof(NpgsqlDbFunctionsExtensions)
        .GetMethod(
            "ILike",
            BindingFlags.Public | BindingFlags.Static,
            [typeof(DbFunctions), typeof(string), typeof(string)]);

    /// <summary>
    /// Builds a combined AND predicate from <paramref name="conditions"/>.
    /// An empty list produces <c>_ =&gt; true</c>.
    /// </summary>
    public static Expression<Func<TEntity, bool>> Build<TEntity>(IReadOnlyList<FilterCondition> conditions)
    {
        if (conditions.Count == 0)
            return _ => true;

        var param = Expression.Parameter(typeof(TEntity), "x");
        var body = conditions
            .Select(c => BuildSingle(param, c))
            .Aggregate(Expression.AndAlso);

        return Expression.Lambda<Func<TEntity, bool>>(body, param);
    }

    /// <summary>
    /// Builds an ORDER BY key selector for the given property name.
    /// Uses reflection so EF Core sees the actual property type (not <c>object</c>).
    /// </summary>
    public static IQueryable<TEntity> ApplyOrderBy<TEntity>(
        IQueryable<TEntity> query, string propertyName, bool descending)
    {
        var param = Expression.Parameter(typeof(TEntity), "x");
        var member = GetMemberExpression(param, propertyName);
        var lambda = Expression.Lambda(member, param);

        var methodName = descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var method = typeof(Queryable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == methodName && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), member.Type);

        return (IQueryable<TEntity>)method.Invoke(null, [query, lambda])!;
    }

    // --------------------------------------------------------------------------
    // Private helpers
    // --------------------------------------------------------------------------

    private static Expression BuildSingle(ParameterExpression param, FilterCondition condition)
    {
        var member = GetMemberExpression(param, condition.PropertyName);
        var memberType = member.Type;

        return condition.Operator switch
        {
            FilterOperator.Equal             => Expression.Equal(member, ToConstant(condition.Value, memberType)),
            FilterOperator.NotEqual          => Expression.NotEqual(member, ToConstant(condition.Value, memberType)),
            FilterOperator.GreaterThan       => Expression.GreaterThan(member, ToConstant(condition.Value, memberType)),
            FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(member, ToConstant(condition.Value, memberType)),
            FilterOperator.LessThan          => Expression.LessThan(member, ToConstant(condition.Value, memberType)),
            FilterOperator.LessThanOrEqual   => Expression.LessThanOrEqual(member, ToConstant(condition.Value, memberType)),
            FilterOperator.Contains          => BuildStringCall(member, condition.Value, nameof(string.Contains)),
            FilterOperator.StartsWith        => BuildStringCall(member, condition.Value, nameof(string.StartsWith)),
            FilterOperator.EndsWith          => BuildStringCall(member, condition.Value, nameof(string.EndsWith)),
            FilterOperator.Like              => BuildILike(member, condition.Value),
            FilterOperator.In                => BuildIn(member, condition.Value, memberType),
            FilterOperator.IsNull            => Expression.Equal(member, Expression.Constant(null, memberType)),
            FilterOperator.IsNotNull         => Expression.NotEqual(member, Expression.Constant(null, memberType)),
            _                                => throw new NotSupportedException(
                                                   $"FilterOperator '{condition.Operator}' is not supported.")
        };
    }

    private static MemberExpression GetMemberExpression(Expression root, string propertyPath)
    {
        Expression expr = root;
        foreach (var part in propertyPath.Split('.'))
            expr = Expression.Property(expr, part);
        return (MemberExpression)expr;
    }

    private static ConstantExpression ToConstant(object? value, Type targetType)
    {
        if (value is null)
            return Expression.Constant(null, targetType);

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;
        return Expression.Constant(Convert.ChangeType(value, underlying), targetType);
    }

    private static MethodCallExpression BuildStringCall(
        MemberExpression member, object? value, string methodName)
    {
        var method = typeof(string).GetMethod(methodName, [typeof(string)])
                     ?? throw new InvalidOperationException(
                         $"string.{methodName}(string) method not found.");
        return Expression.Call(member, method, Expression.Constant(value?.ToString()));
    }

    private static MethodCallExpression BuildILike(MemberExpression member, object? value)
    {
        if (_iLikeMethod is null)
            throw new InvalidOperationException(
                "NpgsqlDbFunctionsExtensions.ILike(DbFunctions, string, string) could not be resolved. " +
                "Ensure Npgsql.EntityFrameworkCore.PostgreSQL is referenced.");

        return Expression.Call(
            _iLikeMethod,
            Expression.Constant(EF.Functions),
            member,
            Expression.Constant(value?.ToString() ?? string.Empty));
    }

    private static MethodCallExpression BuildIn(
        MemberExpression member, object? value, Type memberType)
    {
        if (value is not IEnumerable enumerable)
            throw new ArgumentException(
                "FilterCondition.Value must implement IEnumerable for the 'In' operator.",
                nameof(value));

        // Build a typed List<T> so EF Core generates SQL IN (...).
        var elemType = Nullable.GetUnderlyingType(memberType) ?? memberType;
        var listType = typeof(List<>).MakeGenericType(elemType);
        var list = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in enumerable)
            list.Add(Convert.ChangeType(item, elemType));

        var containsMethod = listType.GetMethod("Contains", [elemType])
                             ?? throw new InvalidOperationException(
                                 $"List<{elemType.Name}>.Contains method not found.");

        // For nullable value types cast to the underlying type; reference types are fine as-is.
        Expression memberExpr = Nullable.GetUnderlyingType(memberType) is not null
            ? Expression.Convert(member, elemType)
            : member;

        return Expression.Call(Expression.Constant(list), containsMethod, memberExpr);
    }
}
