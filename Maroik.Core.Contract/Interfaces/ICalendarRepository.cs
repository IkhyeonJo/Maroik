using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Calendar table.
/// </summary>
public interface ICalendarRepository
{
    /// <summary>Returns every calendar in the database (admin use or shared-calendar discovery).</summary>
    public Task<List<CalendarResponse>> GetAllCalendarsAsync();

    /// <summary>Returns all calendars owned by the account with the given email.</summary>
    public Task<List<CalendarResponse>> GetCalendarsAsync(string email);

    /// <summary>Inserts a new calendar row.</summary>
    public Task CreateCalendarAsync(CalendarRequest request);

    /// <summary>Updates the name, description, colour, and time-zone of an existing calendar.</summary>
    public Task UpdateCalendarAsync(CalendarRequest request);

    /// <summary>Deletes a calendar row and cascades to its events via the database constraint.</summary>
    public Task DeleteCalendarAsync(CalendarRequest request);
}
