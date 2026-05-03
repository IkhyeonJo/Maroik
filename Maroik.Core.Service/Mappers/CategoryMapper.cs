using Maroik.Core.Contract.Domains.Menu;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class CategoryMapper
{
    internal static CategoryResponse ToResponse(Category c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        DisplayName = c.DisplayName,
        IconPath = c.IconPath,
        Controller = c.Controller,
        Action = c.Action,
        Role = c.Role,
        Order = c.Order
    };
}
