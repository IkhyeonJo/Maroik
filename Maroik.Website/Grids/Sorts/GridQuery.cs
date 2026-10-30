using System.Linq.Expressions;

namespace Maroik.Website.Grids.Sorts;

public sealed class GridQuery : ExpressionVisitor
{
    private Boolean Ordered { get; set; }

    private GridQuery()
    {
    }

    public static Boolean IsOrdered(IQueryable models)
    {
        GridQuery expression = new();
        expression.Visit(models.Expression);

        return expression.Ordered;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending))
        {
            Ordered = true;

            return node;
        }

        return base.VisitMethodCall(node);
    }
}
