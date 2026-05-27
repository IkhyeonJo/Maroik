using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

/// <summary>Maps <see cref="CalendarEventAttachedFile"/> domain objects to <see cref="CalendarEventAttachedFileResponse"/> DTOs.</summary>
internal static class CalendarEventAttachedFileMapper
{
    /// <summary>Converts a <see cref="CalendarEventAttachedFile"/> domain object to its corresponding <see cref="CalendarEventAttachedFileResponse"/> DTO.</summary>
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
