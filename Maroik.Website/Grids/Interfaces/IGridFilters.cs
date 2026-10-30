using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Primitives;

namespace Maroik.Website.Grids.Interfaces;

public interface IGridFilters
{
    Func<String> BooleanTrueOptionText { get; set; }
    Func<String> BooleanFalseOptionText { get; set; }
    Func<String> BooleanEmptyOptionText { get; set; }

    IGridFilter? Create(Type type, String method, StringValues values);
    IEnumerable<SelectListItem> OptionsFor<T, TValue>(IGridColumn<T, TValue> column);

    void Register(Type type, String method, Type filter);
    void Unregister(Type type, String method);
}
