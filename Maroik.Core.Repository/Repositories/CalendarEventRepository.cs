using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class CalendarEventRepository : ICalendarEventRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarEventRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all calendar events for the calendar.
    /// </summary>
    /// <param name="calendarId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<CalendarEvent>> GetCalendarEventsAsync(long calendarId)
    {
        return await _context.CalendarEvents.Where(x => x.CalendarId == calendarId).ToListAsync();
    }

    /// <summary>
    /// Gets a calendar event.
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<CalendarEvent> GetCalendarEventAsync(long id)
    {
        return await _context.CalendarEvents.Where(x => x.Id == id).FirstOrDefaultAsync();
    }

    /// <summary>
    /// Creates a calendar event.
    /// </summary>
    /// <param name="calendarEvent"></param>
    /// <returns></returns>
    public async Task CreateCalendarEventAsync(CalendarEvent calendarEvent)
    {
        _ = await _context.CalendarEvents.AddAsync(calendarEvent);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a calendar event.
    /// </summary>
    /// <param name="calendarEvent"></param>
    /// <returns></returns>
    public async Task UpdateCalendarEventAsync(CalendarEvent calendarEvent)
    {
        _ = _context.CalendarEvents.Update(calendarEvent);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    /// <param name="calendarEvent"></param>
    /// <returns></returns>
    public async Task DeleteCalendarEventAsync(CalendarEvent calendarEvent)
    {
        _ = _context.CalendarEvents.Remove(calendarEvent);
        _ = await _context.SaveChangesAsync();
    }
}