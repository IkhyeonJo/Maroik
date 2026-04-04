using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarEventReminderRepository
{
    public Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId);
    public Task CreateCalendarEventReminderAsync(CalendarEventReminderRequest request);
    public Task<int> DeleteCalendarEventReminderAsync(long calendarEventId);
}
