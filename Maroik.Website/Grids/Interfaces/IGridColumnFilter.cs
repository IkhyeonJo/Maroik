using Maroik.Website.Grids.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridColumnFilter
{
    String Name { get; set; }
    Boolean? IsEnabled { get; set; }
    String DefaultMethod { get; set; }
    GridFilterCase? Case { get; set; }
    GridFilterType? Type { get; set; }
    IEnumerable<SelectListItem> Options { get; set; }

    String? Operator { get; set; }
    IGridFilter? First { get; set; }
    IGridFilter? Second { get; set; }
}
