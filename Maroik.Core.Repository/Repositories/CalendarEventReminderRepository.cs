using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmReminder = Maroik.Core.PostgreSQL.Models.CalendarEventReminder;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICalendarEventReminderRepository"/> that persists and
/// retrieves <see cref="CalendarEventReminder"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
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
        // Raw SQL delete removes all reminders for the event atomically in one statement.
        => await context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "CalendarEventReminder" WHERE "CalendarEventId" = {calendarEventId}""");

    /// <summary>
    /// Maps an ORM <see cref="OrmReminder"/> row to the <see cref="CalendarEventReminder"/> domain object.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="CalendarEventReminder"/> domain object.</returns>
    private static CalendarEventReminder ToDomain(OrmReminder e)
        => CalendarEventReminder.Reconstitute(
            e.Id, e.CalendarEventId, e.Method,
            e.MinutesBeforeEvent, e.HoursBeforeEvent,
            e.DaysBeforeEvent, e.WeeksBeforeEvent, e.TimesBeforeEvent);

    /// <summary>
    /// Maps a <see cref="CalendarEventReminder"/> domain object to an ORM <see cref="OrmReminder"/> entity
    /// ready for insertion.
    /// </summary>
    /// <param name="r">The domain object to convert.</param>
    /// <returns>An <see cref="OrmReminder"/> entity suitable for EF Core tracking.</returns>
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
