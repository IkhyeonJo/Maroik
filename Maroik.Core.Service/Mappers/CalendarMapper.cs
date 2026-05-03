using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class CalendarMapper
{
    internal static CalendarResponse ToResponse(Calendar calendar) => new()
    {
        Id = calendar.Id,
        AccountEmail = calendar.AccountEmail.Value,
        Name = calendar.Name,
        Description = calendar.Description,
        TimeZoneIanaId = calendar.TimeZone.Value,
        HtmlColorCode = calendar.ColorCode.Value,
        Created = calendar.Created,
        Updated = calendar.Updated
    };
}
