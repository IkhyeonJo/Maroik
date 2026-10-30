using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.HtmlContents;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Processors;
using Maroik.Website.Grids.Sorts;
using Microsoft.AspNetCore.Html;

namespace Maroik.Website.Grids.Columns;

public class GridColumn<T, TValue> : IGridColumn<T, TValue> where T : class
{
    public IGrid<T> Grid { get; set; }

    public String Name { get; set; }
    public String Style { get; set; }
    public Object Title { get; set; }
    public String? Format { get; set; }
    public Boolean IsHidden { get; set; }
    public String CssClasses { get; set; }
    public Boolean IsEncoded { get; set; }
    public GridProcessorType ProcessorType { get; set; }

    public Func<T, TValue> ExpressionValue { get; set; }
    public Func<T, Int32, Object?>? RenderValue { get; set; }
    public Expression<Func<T, TValue>> Expression { get; set; }

    IGridColumnSort IGridColumn.Sort => Sort;
    IGridColumnSort<T> IGridColumn<T>.Sort => Sort;
    public IGridColumnSort<T, TValue> Sort { get; set; }

    IGridColumnFilter IGridColumn.Filter => Filter;
    IGridColumnFilter<T> IGridColumn<T>.Filter => Filter;
    public IGridColumnFilter<T, TValue> Filter { get; set; }

    public GridColumn(IGrid<T> grid, Expression<Func<T, TValue>> expression)
    {
        Style = "";
        Grid = grid;
        CssClasses = "";
        IsEncoded = true;
        Expression = expression;
        Name = NameFor(expression);
        Title = TitleFor(expression);
        ProcessorType = GridProcessorType.Pre;
        ExpressionValue = expression.Compile();
        Sort = new GridColumnSort<T, TValue>(this);
        Filter = new GridColumnFilter<T, TValue>(this);
    }

    public virtual IQueryable<T> Process(IQueryable<T> items)
    {
        return Filter.Apply(items);
    }
    public virtual IHtmlContent ValueFor(IGridRow<Object> row)
    {
        Object? value = ColumnValueFor(row);

        if (value == null)
            return HtmlString.Empty;

        if (value is IHtmlContent content)
            return content;

        if (Format != null)
            value = String.Format(CultureInfo.CurrentCulture, Format, value);

        if (IsEncoded)
            return new GridHtmlString(value.ToString());

        return new HtmlString(value.ToString());
    }

    private String TitleFor(Expression<Func<T, TValue>> expression)
    {
        MemberExpression? body = expression.Body as MemberExpression;
        DisplayAttribute? display = body?.Member.GetCustomAttribute<DisplayAttribute>();

        return display?.GetShortName() ?? "";
    }
    private String NameFor(Expression<Func<T, TValue>> expression)
    {
        String text = expression.Body is MemberExpression member ? member.ToString() : "";

        return text.Contains('.', StringComparison.Ordinal) ? text[(text.IndexOf('.', StringComparison.Ordinal) + 1)..] : text;
    }
    private Object? ColumnValueFor(IGridRow<Object> row)
    {
        try
        {
            if (RenderValue != null)
                return RenderValue((T)row.Model, row.Index);

            Type type = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);

            if (type.IsEnum)
                return EnumValue(type, ExpressionValue((T)row.Model)!.ToString()!);

            return ExpressionValue((T)row.Model);
        }
        // An expected condition, not a fault: a null part of the column's expression (model.Child.Name without a Child)
        // renders an empty cell, as upstream NonFactors 8.0.0 does.
        catch (NullReferenceException)
        {
            return null;
        }
    }
    private String EnumValue(Type type, String value)
    {
        return type
            .GetMember(value)
            .FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName() ?? value;
    }
}
