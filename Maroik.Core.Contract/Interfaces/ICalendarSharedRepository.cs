using Maroik.Core.Domain.Calendar;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="CalendarShared"/> (sharing-permission) persistence.
/// </summary>
public interface ICalendarSharedRepository : IGenericRepository<CalendarShared>
{
    /// <summary>Returns the sharing-permission rows for all calendars.</summary>
    Task<List<CalendarShared>> GetAllAsync(CancellationToken ct = default);
}
