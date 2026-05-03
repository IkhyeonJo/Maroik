using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class CalendarEventMapper
{
    internal static CalendarEventResponse ToResponse(CalendarEvent evt) => new()
    {
        Id = evt.Id,
        CalendarId = evt.CalendarId,
        Title = evt.Title,
        Description = evt.Description,
        AllDay = evt.AllDay,
        StartDate = evt.StartDate,
        EndDate = evt.EndDate,
        StartDateTimeZoneIanaId = evt.StartDateTimeZoneIanaId,
        EndDateTimeZoneIanaId = evt.EndDateTimeZoneIanaId,
        Location = evt.Location,
        Status = evt.Status,
        RecurrenceId = evt.RecurrenceId,
        Created = evt.Created,
        Updated = evt.Updated
    };
}
