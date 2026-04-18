using Mapster;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Maroik.Core.PostgreSQL.Models;
using Microsoft.EntityFrameworkCore;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarEventReminderRepository"/>.</summary>
public class CalendarEventReminderRepository : ICalendarEventReminderRepository
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="CalendarEventReminderRepository"/> with <paramref name="context"/>.</summary>
    public CalendarEventReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId)
    {
        return (await _context.CalendarEventReminders.Where(x => x.CalendarEventId == calendarEventId).ToListAsync()).Adapt<List<CalendarEventReminderResponse>>();
    }

    /// <inheritdoc />
    public async Task CreateCalendarEventReminderAsync(CalendarEventReminderRequest request)
    {
        _ = await _context.CalendarEventReminders.AddAsync(request.Adapt<CalendarEventReminder>());
        _ = await _context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> DeleteCalendarEventReminderAsync(long calendarEventId)
    {
        return await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "CalendarEventReminder" WHERE "CalendarEventId" = {calendarEventId}"""
        );
    }
}
