using Maroik.Core.Repository.Contracts;
using Maroik.Core.Repository.Data;
using Maroik.Core.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class CalendarSharedRepository : ICalendarSharedRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarSharedRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all shared calendars.
    /// </summary>
    /// <returns></returns>
    public async Task<List<CalendarShared>> GetCalendarSharedAsync()
    {
        return await _context.CalendarShareds.OrderBy(x => x.CalendarId).ToListAsync();
    }

    /// <summary>
    /// Creates a shared calendar.
    /// </summary>
    /// <param name="calendarShared"></param>
    /// <returns></returns>
    public async Task CreateCalendarSharedAsync(CalendarShared calendarShared)
    {
        _ = await _context.CalendarShareds.AddAsync(calendarShared);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates a shared calendar.
    /// </summary>
    /// <param name="calendarShared"></param>
    /// <returns></returns>
    public async Task UpdateCalendarSharedAsync(CalendarShared calendarShared)
    {
        _ = _context.CalendarShareds.Update(calendarShared);
        _ = await _context.SaveChangesAsync();
    }
}