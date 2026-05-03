using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmReminder = Maroik.Core.PostgreSQL.Models.CalendarEventReminder;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarEventReminderRepository"/>.</summary>
public class CalendarEventReminderRepository(ApplicationDbContext context) : ICalendarEventReminderRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEventReminder>> GetCalendarEventRemindersAsync(long calendarEventId)
        => (await context.CalendarEventReminders
            .Where(x => x.CalendarEventId == calendarEventId)
            .ToListAsync())
            .Select(ToDomain);

    /// <inheritdoc />
    public async Task CreateCalendarEventReminderAsync(CalendarEventReminder reminder)
    {
        _ = await context.CalendarEventReminders.AddAsync(ToEntity(reminder));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> DeleteCalendarEventReminderAsync(long calendarEventId)
        => await context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "CalendarEventReminder" WHERE "CalendarEventId" = {calendarEventId}""");

    private static CalendarEventReminder ToDomain(OrmReminder e)
        => CalendarEventReminder.Reconstitute(
            e.Id, e.CalendarEventId, e.Method,
            e.MinutesBeforeEvent, e.HoursBeforeEvent,
            e.DaysBeforeEvent, e.WeeksBeforeEvent, e.TimesBeforeEvent);

    private static OrmReminder ToEntity(CalendarEventReminder r) => new()
    {
        Id = r.Id,
        CalendarEventId = r.CalendarEventId,
        Method = r.Method,
        MinutesBeforeEvent = r.MinutesBeforeEvent,
        HoursBeforeEvent = r.HoursBeforeEvent,
        DaysBeforeEvent = r.DaysBeforeEvent,
        WeeksBeforeEvent = r.WeeksBeforeEvent,
        TimesBeforeEvent = r.TimesBeforeEvent
    };
}
