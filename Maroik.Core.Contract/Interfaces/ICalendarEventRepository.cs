using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarEvent table.
/// </summary>
public interface ICalendarEventRepository
{
    /// <summary>Returns all events belonging to the given calendar.</summary>
    public Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId);

    /// <summary>Returns a single event by its ID, or null.</summary>
    public Task<CalendarEventResponse?> GetCalendarEventAsync(long id);

    /// <summary>Inserts a new calendar event row.</summary>
    public Task CreateCalendarEventAsync(CalendarEventRequest request);

    /// <summary>Updates an existing calendar event's fields.</summary>
    public Task UpdateCalendarEventAsync(CalendarEventRequest request);

    /// <summary>Deletes a calendar event row (hard delete; reminders are cascaded by the DB constraint).</summary>
    public Task DeleteCalendarEventAsync(CalendarEventRequest request);
}
