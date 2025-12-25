using Maroik.Core.Repository.Contracts;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class CalendarRepository : ICalendarRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all calendars.
    /// </summary>
    /// <returns></returns>
    public async Task<List<Calendar>> GetAllCalendarsAsync()
    {
        return await _context.Calendars.OrderBy(x => x.Name).ToListAsync();
    }

    /// <summary>
    /// Gets all calendars for the logged-in account.
    /// </summary>
    /// <param name="email"></param>
    /// <returns></returns>
    public async Task<List<Calendar>> GetCalendarsAsync(string email)
    {
        return await _context.Calendars.Where(x => x.AccountEmail == email).OrderBy(x => x.Name).ToListAsync();
    }

    /// <summary>
    /// Creates a calendar.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public async Task CreateCalendarAsync(Calendar calendar)
    {
        _ = await _context.Calendars.AddAsync(calendar);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a calendar.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public async Task UpdateCalendarAsync(Calendar calendar)
    {
        _ = _context.Calendars.Update(calendar);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes a calendar.
    /// </summary>
    /// <param name="calendar"></param>
    /// <returns></returns>
    public async Task DeleteCalendarAsync(Calendar calendar)
    {
        _ = _context.Calendars.Remove(calendar);
        _ = await _context.SaveChangesAsync();
    }
}