using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarEvent = Maroik.Core.PostgreSQL.Models.CalendarEvent;

namespace Maroik.Core.Repository.Repositories;

public class CalendarEventRepository(ApplicationDbContext context)
    : GenericRepository<CalendarEvent, OrmCalendarEvent>(context), ICalendarEventRepository
{
    protected override DbSet<OrmCalendarEvent> Set => Context.CalendarEvents;

    /// <inheritdoc />
    public async Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent)
    {
        OrmCalendarEvent entity = ToEntity(calendarEvent);
        _ = await Context.CalendarEvents.AddAsync(entity);
        _ = await Context.SaveChangesAsync();
        return entity.Id;
    }

    protected override CalendarEvent ToDomain(OrmCalendarEvent e) => CalendarEvent.Reconstitute(
        e.Id, e.CalendarId, e.Title ?? string.Empty, e.Description,
        e.AllDay, e.StartDate, e.EndDate,
        e.StartDateTimeZoneIanaId, e.EndDateTimeZoneIanaId,
        e.Location, e.Status, e.RecurrenceId, e.Created, e.Updated);

    protected override OrmCalendarEvent ToEntity(CalendarEvent ce) => new()
    {
        Id = ce.Id,
        CalendarId = ce.CalendarId,
        Title = ce.Title,
        Description = ce.Description,
        AllDay = ce.AllDay,
        StartDate = ce.StartDate,
        EndDate = ce.EndDate,
        StartDateTimeZoneIanaId = ce.StartDateTimeZoneIanaId,
        EndDateTimeZoneIanaId = ce.EndDateTimeZoneIanaId,
        Location = ce.Location,
        Status = ce.Status,
        RecurrenceId = ce.RecurrenceId,
        Created = ce.Created,
        Updated = ce.Updated
    };
}
