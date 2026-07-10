using Maroik.Core.Contract.Domains.Calendar;
namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Repository abstraction over <see cref="Calendar"/> persistence.
/// </summary>
public interface ICalendarRepository : IGenericRepository<Calendar>
{
    /// <summary>Returns calendars owned by the given account.</summary>
    Task<List<Calendar>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);

    /// <summary>Returns every calendar in the system, ordered by name.</summary>
    Task<List<Calendar>> GetAllOrderedByNameAsync(CancellationToken ct = default);

    /// <summary>Deletes the calendar with the given ID.</summary>
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
