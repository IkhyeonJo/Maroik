using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarEventRepository
{
    /// <summary>
    /// Gets all calendar events for the calendar.
    /// </summary>
    /// <param name="calendarId"></param>
    /// <returns></returns>
    public Task<IEnumerable<CalendarEvent>> GetCalendarEventsAsync(long calendarId);

    /// <summary>
    /// Gets a calendar event.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<CalendarEvent> GetCalendarEventAsync(long id);

    /// <summary>
    /// Creates a calendar event.
    /// </summary>
    /// <param name="calendarEvent"></param>
    /// <returns></returns>
    public Task CreateCalendarEventAsync(CalendarEvent calendarEvent);

    /// <summary>
    /// Updates a calendar event.
    /// </summary>
    /// <param name="calendarEvent"></param>
    /// <returns></returns>
    public Task UpdateCalendarEventAsync(CalendarEvent calendarEvent);

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    /// <param name="calendarEvent"></param>
    /// <returns></returns>
    public Task DeleteCalendarEventAsync(CalendarEvent calendarEvent);
}