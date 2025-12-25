using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface IOtherCalendarRepository
{
    /// <summary>
    /// Gets all other calendars.
    /// </summary>
    /// <returns></returns>
    public Task<List<OtherCalendar>> GetAllOtherCalendarsAsync();

    /// <summary>
    /// Gets all other calendars for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<OtherCalendar>> GetOtherCalendarsAsync(string email);

    /// <summary>
    /// Creates an other calendar.
    /// </summary>
    /// <param name="otherCalendar"></param>
    /// <returns></returns>
    public Task CreateOtherCalendarAsync(OtherCalendar otherCalendar);

    /// <summary>
    /// Deletes an other calendar.
    /// </summary>
    /// <param name="otherCalendar"></param>
    /// <returns></returns>
    public Task DeleteOtherCalendarAsync(OtherCalendar otherCalendar);
}