using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarEventReminder table.
/// A calendar event can have multiple reminders with different lead times and methods.
/// </summary>
public interface ICalendarEventReminderRepository
{
    /// <summary>Returns all reminders configured for the given calendar event.</summary>
    public Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId);

    /// <summary>Inserts a new reminder row for a calendar event.</summary>
    public Task CreateCalendarEventReminderAsync(CalendarEventReminderRequest request);

    /// <summary>Deletes all reminder rows for the given calendar event. Returns the number of rows deleted.</summary>
    public Task<int> DeleteCalendarEventReminderAsync(long calendarEventId);
}
