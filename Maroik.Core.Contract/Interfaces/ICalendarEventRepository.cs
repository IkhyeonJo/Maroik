using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarEvent table.
/// </summary>
public interface ICalendarEventRepository
{
    /// <summary>Returns all events belonging to the given calendar.</summary>
    public Task<IEnumerable<CalendarEvent>> GetCalendarEventsAsync(long calendarId);

    /// <summary>Returns a single event by its ID, or null.</summary>
    public Task<CalendarEvent?> GetCalendarEventAsync(long id);

    /// <summary>Inserts a new calendar event row and returns the database-generated ID.</summary>
    public Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent);

    /// <summary>Updates an existing calendar event's fields.</summary>
    public Task UpdateCalendarEventAsync(CalendarEvent calendarEvent);

    /// <summary>Deletes a calendar event row (hard delete; reminders are cascaded by the DB constraint).</summary>
    public Task DeleteCalendarEventAsync(CalendarEvent calendarEvent);
}
