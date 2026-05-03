using Maroik.Core.Contract.Domains.Calendar;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarShared table.
/// Each calendar has exactly one CalendarShared row controlling its public visibility.
/// </summary>
public interface ICalendarSharedRepository
{
    /// <summary>Returns sharing-permission rows for all calendars.</summary>
    public Task<List<CalendarShared>> GetCalendarSharedAsync();

    /// <summary>Inserts a new CalendarShared row (called when a calendar is first created).</summary>
    public Task CreateCalendarSharedAsync(CalendarShared shared);

    /// <summary>Updates the User and Anonymous visibility flags for the given calendar.</summary>
    public Task UpdateCalendarSharedAsync(CalendarShared shared);
}
