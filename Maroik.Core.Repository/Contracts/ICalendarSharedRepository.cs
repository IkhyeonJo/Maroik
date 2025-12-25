using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface ICalendarSharedRepository
{
    /// <summary>
    /// Gets all shared calendars.
    /// </summary>
    /// <returns></returns>
    public Task<List<CalendarShared>> GetCalendarSharedAsync();

    /// <summary>
    /// Creates a shared calendar.
    /// </summary>
    /// <param name="calendarShared"></param>
    /// <returns></returns>
    public Task CreateCalendarSharedAsync(CalendarShared calendarShared);

    /// <summary>
    /// Updates a shared calendar.
    /// </summary>
    /// <param name="calendarShared"></param>
    /// <returns></returns>
    public Task UpdateCalendarSharedAsync(CalendarShared calendarShared);
}