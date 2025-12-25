using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface ICalendarRepository
{
    /// <summary>
    /// Gets all calendars.
    /// </summary>
    /// <returns></returns>
    public Task<List<Calendar>> GetAllCalendarsAsync();

    /// <summary>
    /// Gets all calendars for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public Task<List<Calendar>> GetCalendarsAsync(string email);

    /// <summary>
    /// Creates a calendar.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public Task CreateCalendarAsync(Calendar calendar);

    /// <summary>
    /// Updates a calendar.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public Task UpdateCalendarAsync(Calendar calendar);

    /// <summary>
    /// Deletes a calendar.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public Task DeleteCalendarAsync(Calendar calendar);
}