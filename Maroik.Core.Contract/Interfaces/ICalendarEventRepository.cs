using Maroik.Core.Domain.Calendar;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="CalendarEvent"/> persistence.
/// </summary>
public interface ICalendarEventRepository : IGenericRepository<CalendarEvent>
{
    /// <summary>Inserts a new calendar event and returns its generated ID.</summary>
    Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default);

    /// <summary>Returns all events belonging to any of the given calendars, in a single query.</summary>
    Task<List<CalendarEvent>> GetByCalendarIdsAsync(IEnumerable<long> calendarIds, CancellationToken ct = default);

    /// <summary>Returns the calendar event with the given ID, or null if not found.</summary>
    Task<CalendarEvent?> FindByIdAsync(long id, CancellationToken ct = default);

    /// <summary>Deletes the calendar event with the given ID.</summary>
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
