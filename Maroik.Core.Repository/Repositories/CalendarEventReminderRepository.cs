using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

public class CalendarEventReminderRepository : ICalendarEventReminderRepository
{
    private readonly ApplicationDbContext _context;

    public CalendarEventReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId)
    {
        return (await _context.CalendarEventReminders.Where(x => x.CalendarEventId == calendarEventId).ToListAsync()).Adapt<List<CalendarEventReminderResponse>>();
    }

    public async Task CreateCalendarEventReminderAsync(CalendarEventReminderRequest request)
    {
        _ = await _context.CalendarEventReminders.AddAsync(request.Adapt<CalendarEventReminder>());
        _ = await _context.SaveChangesAsync();
    }

    public async Task<int> DeleteCalendarEventReminderAsync(long calendarEventId)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "CalendarEventReminder" WHERE "CalendarEventId" = {calendarEventId}"""
        );
    }
}
