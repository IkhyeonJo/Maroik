using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

internal static class CalendarEventReminderMapper
{
    internal static CalendarEventReminderResponse ToResponse(CalendarEventReminder reminder) => new()
    {
        Id = reminder.Id,
        CalendarEventId = reminder.CalendarEventId,
        Method = reminder.Method,
        MinutesBeforeEvent = reminder.MinutesBeforeEvent,
        HoursBeforeEvent = reminder.HoursBeforeEvent,
        DaysBeforeEvent = reminder.DaysBeforeEvent,
        WeeksBeforeEvent = reminder.WeeksBeforeEvent,
        TimesBeforeEvent = reminder.TimesBeforeEvent
    };
}
