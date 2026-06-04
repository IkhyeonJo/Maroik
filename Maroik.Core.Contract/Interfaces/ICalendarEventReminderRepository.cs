using Maroik.Core.Contract.Domains.Calendar;
namespace Maroik.Core.Contract.Interfaces;
public interface ICalendarEventReminderRepository : IGenericRepository<CalendarEventReminder>
{
    Task<List<CalendarEventReminder>> GetByCalendarEventIdAsync(long calendarEventId, CancellationToken ct = default);
    Task DeleteByCalendarEventIdAsync(long calendarEventId, CancellationToken ct = default);
}
