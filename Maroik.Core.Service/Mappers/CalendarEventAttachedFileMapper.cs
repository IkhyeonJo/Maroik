using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class CalendarEventAttachedFileMapper
{
    internal static CalendarEventAttachedFileResponse ToResponse(CalendarEventAttachedFile file) => new()
    {
        Id = file.Id,
        CalendarEventId = file.CalendarEventId,
        Size = file.Size,
        Name = file.Name,
        Extension = file.Extension,
        Path = file.Path
    };
}
