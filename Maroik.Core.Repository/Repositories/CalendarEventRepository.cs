using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarEvent = Maroik.Core.PostgreSQL.Models.CalendarEvent;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarEventRepository"/>.</summary>
public class CalendarEventRepository(ApplicationDbContext context) : ICalendarEventRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<CalendarEvent>> GetCalendarEventsAsync(long calendarId)
        => (await context.CalendarEvents.Where(x => x.CalendarId == calendarId).ToListAsync()).Select(ToDomain);

    /// <inheritdoc />
    public async Task<CalendarEvent?> GetCalendarEventAsync(long id)
    {
        OrmCalendarEvent? entity = await context.CalendarEvents.Where(x => x.Id == id).FirstOrDefaultAsync();
        return entity == null ? null : ToDomain(entity);
    }

    /// <inheritdoc />
    public async Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent)
    {
        OrmCalendarEvent entity = ToEntity(calendarEvent);
        _ = await context.CalendarEvents.AddAsync(entity);
        _ = await context.SaveChangesAsync();
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task UpdateCalendarEventAsync(CalendarEvent calendarEvent)
    {
        _ = context.CalendarEvents.Update(ToEntity(calendarEvent));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteCalendarEventAsync(CalendarEvent calendarEvent)
    {
        _ = context.CalendarEvents.Remove(ToEntity(calendarEvent));
        _ = await context.SaveChangesAsync();
    }

    private static CalendarEvent ToDomain(OrmCalendarEvent e) => CalendarEvent.Reconstitute(
        e.Id, e.CalendarId, e.Title ?? string.Empty, e.Description,
        e.AllDay, e.StartDate, e.EndDate,
        e.StartDateTimeZoneIanaId, e.EndDateTimeZoneIanaId,
        e.Location, e.Status, e.RecurrenceId, e.Created, e.Updated);

    private static OrmCalendarEvent ToEntity(CalendarEvent ce) => new()
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
