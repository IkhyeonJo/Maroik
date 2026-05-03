using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the Calendar table.
/// </summary>
public interface ICalendarRepository
{
    /// <summary>Returns every calendar in the database (admin use or shared-calendar discovery).</summary>
    public Task<List<Calendar>> GetAllCalendarsAsync();

    /// <summary>Returns all calendars owned by the account with the given email.</summary>
    public Task<List<Calendar>> GetCalendarsAsync(string email);

    /// <summary>Inserts a new calendar row.</summary>
    public Task CreateCalendarAsync(Calendar calendar);

    /// <summary>Updates the name, description, colour, and time-zone of an existing calendar.</summary>
    public Task UpdateCalendarAsync(Calendar calendar);

    /// <summary>Deletes a calendar row and cascades to its events via the database constraint.</summary>
    public Task DeleteCalendarAsync(Calendar calendar);
}
