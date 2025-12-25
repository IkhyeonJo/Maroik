using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class OtherCalendarRepository : IOtherCalendarRepository
{
    private readonly ApplicationDbContext _context;

    public OtherCalendarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all other calendars.
    /// </summary>
    /// <returns></returns>
    public async Task<List<OtherCalendar>> GetAllOtherCalendarsAsync()
    {
        return await _context.OtherCalendars.OrderBy(x => x.CalendarId).ToListAsync();
    }

    /// <summary>
    /// Gets all other calendars for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<OtherCalendar>> GetOtherCalendarsAsync(string email)
    {
        return await _context.OtherCalendars.Where(x => x.AccountEmail == email).OrderBy(x => x.CalendarId).ToListAsync();
    }

    /// <summary>
    /// Creates an other calendar.
    /// </summary>
    /// <param name="otherCalendar"></param>
    /// <returns></returns>
    public async Task CreateOtherCalendarAsync(OtherCalendar otherCalendar)
    {
        _ = await _context.OtherCalendars.AddAsync(otherCalendar);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an other calendar.
    /// </summary>
    /// <param name="otherCalendar"></param>
    /// <returns></returns>
    public async Task DeleteOtherCalendarAsync(OtherCalendar otherCalendar)
    {
        _ = _context.OtherCalendars.Remove(otherCalendar);
        _ = await _context.SaveChangesAsync();
    }
}