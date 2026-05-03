using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class SubCategoryMapper
{
    internal static SubCategoryResponse ToResponse(SubCategory s) => new()
    {
        Id = s.Id,
        CategoryId = s.CategoryId,
        Name = s.Name,
        DisplayName = s.DisplayName,
        IconPath = s.IconPath,
        Action = s.Action,
        Role = s.Role,
        Order = s.Order
    };
}
