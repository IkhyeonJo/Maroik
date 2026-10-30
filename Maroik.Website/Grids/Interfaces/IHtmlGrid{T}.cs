using Microsoft.AspNetCore.Html;

namespace Maroik.Website.Grids.Interfaces;

public interface IHtmlGrid<T>
{
    IGrid<T> Grid { get; }

    String PartialViewName { get; set; }

    Task<IHtmlContent> RenderAsync();
}
