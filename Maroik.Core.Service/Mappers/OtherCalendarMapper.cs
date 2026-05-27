using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

/// <summary>Maps <see cref="OtherCalendar"/> domain objects to <see cref="OtherCalendarResponse"/> DTOs.</summary>
internal static class OtherCalendarMapper
{
    /// <summary>Converts an <see cref="OtherCalendar"/> domain object to its corresponding <see cref="OtherCalendarResponse"/> DTO.</summary>
    internal static OtherCalendarResponse ToResponse(OtherCalendar other) => new()
    {
        AccountEmail = other.AccountEmail.Value,
        CalendarId = other.CalendarId
    };
}
