using Maroik.Website.Grids.Filters;
using Maroik.Website.Grids.Interfaces;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Maroik.Website.Grids.Extensions;

public static class GridExtensions
{
    public static HtmlGrid<T> Grid<T>(this IHtmlHelper html, IEnumerable<T> source) where T : class
    {
        return new HtmlGrid<T>(html, new Grid<T>(source));
    }
    public static HtmlGrid<T> Grid<T>(this IHtmlHelper html, String partialViewName, IEnumerable<T> source) where T : class
    {
        return new HtmlGrid<T>(html, new Grid<T>(source)) { PartialViewName = partialViewName };
    }

    public static IHtmlContent AjaxGrid(this IHtmlHelper _, String url, Object? htmlAttributes = null)
    {
        TagBuilder grid = new("div");
        grid.MergeAttributes(HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes));
        grid.Attributes["data-url"] = url;
        grid.AddCssClass("mvc-grid");

        return grid;
    }

    public static IServiceCollection AddMvcGrid(this IServiceCollection services, Action<GridFilters>? configure = null)
    {
        GridFilters filters = new();
        configure?.Invoke(filters);

        return services.AddSingleton<IGridFilters>(filters);
    }
}
