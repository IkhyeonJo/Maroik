using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmReminder = Maroik.Core.PostgreSQL.Models.CalendarEventReminder;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="CalendarEventReminder"/> domain objects.
/// </summary>
public class CalendarEventReminderRepository(ApplicationDbContext context)
    : GenericRepository<CalendarEventReminder, OrmReminder>(context), ICalendarEventReminderRepository
{
    protected override DbSet<OrmReminder> Set => Context.CalendarEventReminders;

    protected override CalendarEventReminder ToDomain(OrmReminder e) => CalendarEventReminder.Reconstitute(
        e.Id, e.CalendarEventId, e.Method,
        e.MinutesBeforeEvent, e.HoursBeforeEvent,
        e.DaysBeforeEvent, e.WeeksBeforeEvent, e.TimesBeforeEvent);

    protected override OrmReminder ToEntity(CalendarEventReminder r) => new()
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
