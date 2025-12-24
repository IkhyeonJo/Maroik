using Maroik.Core.Repository.Contracts;
using Maroik.Core.Repository.Data;
using Maroik.Core.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class CalendarEventAttachedFileRepository : ICalendarEventAttachedFileRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarEventAttachedFileRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    /// <summary>
    /// Gets all registered files.
    /// </summary>
    /// <returns></returns>
    public async Task<List<CalendarEventAttachedFile>> GetAllAsync()
    {
        return await _context.CalendarEventAttachedFiles.ToListAsync();
    }

    /// <summary>
    /// Gets an attached file.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public async Task<CalendarEventAttachedFile> GetCalendarEventAttachedFileAsync(long calendarEventId)
    {
        return await _context.CalendarEventAttachedFiles.Where(x => x.CalendarEventId == calendarEventId).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Saves an attached file.
    /// </summary>
    /// <param name="calendarEventAttachedFile"></param>
    /// <returns></returns>
    public async Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFile calendarEventAttachedFile)
    {
        _ = await _context.CalendarEventAttachedFiles.AddAsync(calendarEventAttachedFile);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates an attached file.
    /// </summary>
    /// <param name="calendarEventAttachedFile"></param>
    /// <returns></returns>
    public async Task UpdateCalendarEventAttachedFileAsync(CalendarEventAttachedFile calendarEventAttachedFile)
    {
        _ = _context.CalendarEventAttachedFiles.Update(calendarEventAttachedFile);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes attached files.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public async Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync
        (
            $"""DELETE FROM "CalendarEventAttachedFile" WHERE "CalendarEventId" = {calendarEventId}"""
        );
    }
}