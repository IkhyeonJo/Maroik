using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarEvent table.
/// Lookup/delete operations are provided by <see cref="IGenericRepository{TDomain}"/>.
/// </summary>
public interface ICalendarEventRepository : IGenericRepository<CalendarEvent>
{
    /// <summary>Inserts a new calendar event row and returns the database-generated ID.</summary>
    Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default);
}
