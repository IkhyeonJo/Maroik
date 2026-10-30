using System.Globalization;
using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.HtmlContents;
using Maroik.Website.Grids.Interfaces;
using Maroik.Website.Grids.Sorts;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Maroik.Website.Grids.Extensions;

public static class GridColumnExtensions
{
    public static IGridColumn<T, TValue> RenderedAs<T, TValue>(this IGridColumn<T, TValue> column, Func<T, Int32, Object?> value)
    {
        column.RenderValue = value;

        return column;
    }
    public static IGridColumn<T, TValue> RenderedAs<T, TValue>(this IGridColumn<T, TValue> column, Func<T, Object?> value)
    {
        column.RenderValue = (t, _) => value(t);

        return column;
    }

    public static IGridColumn<T, TValue> UsingFilterOptions<T, TValue>(this IGridColumn<T, TValue> column, IEnumerable<SelectListItem>? options = null)
    {
        if (String.IsNullOrEmpty(column.Filter.DefaultMethod))
            column.Filter.DefaultMethod = "equals";

        column.Filter.IsEnabled ??= true;

        column.Filter.Options = options ?? new[] { new SelectListItem() }
            .Concat(column
            .Grid
            .Source
            .OrderBy(column.Expression)
            .Select(column.Expression)
            .Distinct()
            .AsEnumerable()
            .Where(value => value != null)
            .Select(value => value!.ToString())
            .Select(value => new SelectListItem
            {
                Value = value,
                Text = value
            }).ToArray());

        return column;
    }
    public static IGridColumn<T, TValue> UsingDefaultFilterMethod<T, TValue>(this IGridColumn<T, TValue> column, String method)
    {
        column.Filter.DefaultMethod = method;
        column.Filter.IsEnabled ??= true;

        return column;
    }

    public static IGridColumn<T, TValue> Filterable<T, TValue>(this IGridColumn<T, TValue> column, Action<IGridColumnFilter<T, TValue>> configure)
    {
        column.Filter.IsEnabled ??= true;
        configure(column.Filter);

        return column;
    }
    public static IGridColumn<T, TValue> Filterable<T, TValue>(this IGridColumn<T, TValue> column, GridFilterCase filterCase)
    {
        column.Filter.IsEnabled ??= true;
        column.Filter.Case = filterCase;

        return column;
    }
    public static IGridColumn<T, TValue> Filterable<T, TValue>(this IGridColumn<T, TValue> column, Boolean isFilterable)
    {
        column.Filter.IsEnabled = isFilterable;

        return column;
    }
    public static IGridColumn<T, TValue> Filterable<T, TValue>(this IGridColumn<T, TValue> column, GridFilterType type)
    {
        column.Filter.IsEnabled ??= true;
        column.Filter.Type = type;

        return column;
    }
    public static IGridColumn<T, TValue> Filterable<T, TValue>(this IGridColumn<T, TValue> column, String filterName)
    {
        column.Filter.IsEnabled ??= true;
        column.Filter.Name = filterName;

        return column;
    }

    public static IGridColumn<T, TValue> Sortable<T, TValue>(this IGridColumn<T, TValue> column, GridSortOrder firstOrder)
    {
        column.Sort.FirstOrder = firstOrder;
        column.Sort.IsEnabled ??= true;

        return column;
    }
    public static IGridColumn<T, TValue> Sortable<T, TValue>(this IGridColumn<T, TValue> column, Boolean isSortable)
    {
        column.Sort.IsEnabled = isSortable;

        return column;
    }

    public static IGridColumn<T, TValue> Encoded<T, TValue>(this IGridColumn<T, TValue> column, Boolean isEncoded)
    {
        column.IsEncoded = isEncoded;

        return column;
    }
    public static IGridColumn<T, TValue> Formatted<T, TValue>(this IGridColumn<T, TValue> column, String? format)
    {
        column.Format = format;

        return column;
    }
    public static IGridColumn<T, TValue> AppendCss<T, TValue>(this IGridColumn<T, TValue> column, String css)
    {
        column.CssClasses = (column.CssClasses + " " + css.Trim()).Trim();

        return column;
    }
    public static IGridColumn<T, TValue> Titled<T, TValue>(this IGridColumn<T, TValue> column, Object title)
    {
        column.Title = title;

        return column;
    }
    public static IGridColumn<T, TValue> Named<T, TValue>(this IGridColumn<T, TValue> column, String value)
    {
        column.Name = value;

        return column;
    }
    public static IGridColumn<T, TValue> Width<T, TValue>(this IGridColumn<T, TValue> column, String value)
    {
        column.Style = $"width: {value.Split(';')[0]}";

        return column;
    }
    public static IGridColumn<T, TValue> Width<T, TValue>(this IGridColumn<T, TValue> column, Int32 value)
    {
        column.Style = $"width: {value.ToString(CultureInfo.InvariantCulture)}px";

        return column;
    }
    public static IGridColumn<T, TValue> Css<T, TValue>(this IGridColumn<T, TValue> column, String value)
    {
        column.CssClasses = value.Trim();

        return column;
    }

    public static IGridColumn<T, TValue> Hidden<T, TValue>(this IGridColumn<T, TValue> column)
    {
        column.IsHidden = true;

        return column;
    }
    public static GridHtmlAttributes AsAttributes(this IGridColumn column)
    {
        GridHtmlAttributes attributes = new();
        String classes = column.CssClasses;

        attributes["class"] = classes;

        if (!String.IsNullOrEmpty(column.Name))
            attributes["data-name"] = column.Name;

        if (!String.IsNullOrEmpty(column.Style))
            attributes["style"] = column.Style;

        if (column.Filter.IsEnabled == true)
        {
            classes += " filterable";

            if (!String.IsNullOrEmpty(column.Filter.Name))
                attributes["data-filter"] = column.Filter.Name;

            if (column.Filter.Type is GridFilterType type)
                attributes["data-filter-type"] = type;

            if ((column.Filter.First ?? column.Filter.Second) != null)
                attributes["data-filter-applied"] = true;

            if (!String.IsNullOrEmpty(column.Filter.DefaultMethod))
                attributes["data-filter-default-method"] = column.Filter.DefaultMethod;
        }

        if (column.Sort.IsEnabled == true)
        {
            classes += " sortable";

            if (column.Sort.Order is GridSortOrder order)
            {
                attributes["data-sort"] = order;
                classes += $" {order.ToString().ToLowerInvariant()}";
            }

            if (column.Sort.FirstOrder != GridSortOrder.Asc)
                attributes["data-sort-first"] = column.Sort.FirstOrder;
        }

        if (column.IsHidden)
            classes += " mvc-grid-hidden";

        classes = classes.Trim();

        if (String.IsNullOrEmpty(classes))
            attributes.Remove("class");
        else
            attributes["class"] = classes;

        return attributes;
    }
}
