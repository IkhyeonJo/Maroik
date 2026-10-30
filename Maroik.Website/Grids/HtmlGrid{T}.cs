using Maroik.Website.Grids.Interfaces;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Maroik.Website.Grids;

public class HtmlGrid<T> : IHtmlGrid<T>
{
    public IGrid<T> Grid { get; set; }
    public IHtmlHelper Html { get; set; }
    public String PartialViewName { get; set; }

    public HtmlGrid(IHtmlHelper html, IGrid<T> grid)
    {
        grid.HttpContext ??= html.ViewContext.HttpContext;
        grid.Query ??= grid.HttpContext.Request.Query;
        PartialViewName = "MvcGrid/_Grid";
        Html = html;
        Grid = grid;
    }

    // Upstream (grid 8.0.0, Grids/LICENSE.md) made the grid an IHtmlContent whose WriteTo rendered the partial with the synchronous
    // Html.Partial, which blocks on the async view rendering (MVC1000, a build error here, as is every sync-over-async API).
    // The views await this instead: @(await Html.Grid(...)....RenderAsync()).
    public Task<IHtmlContent> RenderAsync()
    {
        return Html.PartialAsync(PartialViewName, Grid);
    }
}
