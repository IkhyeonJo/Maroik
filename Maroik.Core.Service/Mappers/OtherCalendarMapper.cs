using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class OtherCalendarMapper
{
    internal static OtherCalendarResponse ToResponse(OtherCalendar other) => new()
    {
        AccountEmail = other.AccountEmail.Value,
        CalendarId = other.CalendarId
    };
}
