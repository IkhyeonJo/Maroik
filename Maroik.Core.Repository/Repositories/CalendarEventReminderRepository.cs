using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maroik.Core.Repository.Repositories;

public class CalendarEventReminderRepository : ICalendarEventReminderRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarEventReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets all calendar event reminders for the calendar event.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public async Task<IEnumerable<CalendarEventReminder>> GetCalendarEventRemindersAsync(long calendarEventId)
    {
        return await _context.CalendarEventReminders.Where(x => x.CalendarEventId == calendarEventId).ToListAsync();
    }

    /// <summary>
    /// Creates a calendar event reminder.
    /// </summary>
    /// <param name="calendarEventReminder"></param>
    /// <returns></returns>
    public async Task CreateCalendarEventReminderAsync(CalendarEventReminder calendarEventReminder)
    {
        _ = await _context.CalendarEventReminders.AddAsync(calendarEventReminder);
        _ = await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes calendar event reminders.
    /// </summary>
    /// <param name="calendarEventId"></param>
    /// <returns></returns>
    public async Task<int> DeleteCalendarEventReminderAsync(long calendarEventId)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "CalendarEventReminder" WHERE "CalendarEventId" = {calendarEventId}"""
        );
    }
}