using Maroik.Common.DataAccess.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Common.DataAccess.Contracts;

public interface ICalendarEventReminderRepository
{
    /// <summary>
    /// Gets all calendar event reminders for the calendar event.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public Task<IEnumerable<CalendarEventReminder>> GetCalendarEventRemindersAsync(long calendarEventId);

    /// <summary>
    /// Creates a calendar event reminder.
    /// </summary>
    /// <param name="calendarEventReminder"></param>
    /// <returns></returns>
    public Task CreateCalendarEventReminderAsync(CalendarEventReminder calendarEventReminder);

    /// <summary>
    /// Deletes calendar event reminders.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public Task<int> DeleteCalendarEventReminderAsync(long calendarEventId);
}