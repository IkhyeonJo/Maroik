using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Service.Mappers;

/// <summary>Maps <see cref="CalendarEventReminder"/> domain objects to <see cref="CalendarEventReminderResponse"/> DTOs.</summary>
internal static class CalendarEventReminderMapper
{
    /// <summary>Converts a <see cref="CalendarEventReminder"/> domain object to its corresponding <see cref="CalendarEventReminderResponse"/> DTO.</summary>
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
