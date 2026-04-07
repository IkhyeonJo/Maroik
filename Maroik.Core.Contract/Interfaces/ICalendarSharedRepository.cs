using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarShared table.
/// Each calendar has exactly one CalendarShared row controlling its public visibility.
/// </summary>
public interface ICalendarSharedRepository
{
    /// <summary>Returns sharing-permission rows for all calendars.</summary>
    public Task<List<CalendarSharedResponse>> GetCalendarSharedAsync();

    /// <summary>Inserts a new CalendarShared row (called when a calendar is first created).</summary>
    public Task CreateCalendarSharedAsync(CalendarSharedRequest request);

    /// <summary>Updates the User and Anonymous visibility flags for the given calendar.</summary>
    public Task UpdateCalendarSharedAsync(CalendarSharedRequest request);
}
