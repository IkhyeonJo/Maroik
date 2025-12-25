using Maroik.Core.PostgreSQL.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Contracts;

public interface ICalendarEventAttachedFileRepository
{
    /// <summary>
    /// Gets all registered files.
    /// </summary>
    /// <returns></returns>
    public Task<List<CalendarEventAttachedFile>> GetAllAsync();

    /// <summary>
    /// Gets an attached file.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public Task<CalendarEventAttachedFile> GetCalendarEventAttachedFileAsync(long calendarEventId);

    /// <summary>
    /// Saves an attached file.
    /// </summary>
    /// <param name="calendarEventAttachedFile"></param>
    /// <returns></returns>
    public Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFile calendarEventAttachedFile);

    /// <summary>
    /// Updates an attached file.
    /// </summary>
    /// <param name="calendarEventAttachedFile"></param>
    /// <returns></returns>
    public Task UpdateCalendarEventAttachedFileAsync(CalendarEventAttachedFile calendarEventAttachedFile);

    /// <summary>
    /// Deletes attached files.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId);
}