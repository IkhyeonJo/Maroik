using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarEvent = Maroik.Core.PostgreSQL.Models.CalendarEvent;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICalendarEventRepository"/> that persists and retrieves
/// <see cref="CalendarEvent"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
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
        // The database generates the Id; return it so callers can create related records.
        return entity.Id;
    }

    /// <inheritdoc />
    public async Task UpdateCalendarEventAsync(CalendarEvent calendarEvent)
    {
        var existing = await context.CalendarEvents.FindAsync(calendarEvent.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(ToEntity(calendarEvent));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteCalendarEventAsync(CalendarEvent calendarEvent)
    {
        // Hard-delete: the event row is physically removed from the database.
        var existing = await context.CalendarEvents.FindAsync(calendarEvent.Id);
        if (existing == null) return;
        _ = context.CalendarEvents.Remove(existing);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmCalendarEvent"/> row to the <see cref="CalendarEvent"/> domain object,
    /// applying a null-coalescing default for the required Title field.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="CalendarEvent"/> domain object.</returns>
    private static CalendarEvent ToDomain(OrmCalendarEvent e) => CalendarEvent.Reconstitute(
        e.Id, e.CalendarId, e.Title ?? string.Empty, e.Description,
        e.AllDay, e.StartDate, e.EndDate,
        e.StartDateTimeZoneIanaId, e.EndDateTimeZoneIanaId,
        e.Location, e.Status, e.RecurrenceId, e.Created, e.Updated);

    /// <summary>
    /// Maps a <see cref="CalendarEvent"/> domain object to an ORM <see cref="OrmCalendarEvent"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="ce">The domain object to convert.</param>
    /// <returns>An <see cref="OrmCalendarEvent"/> entity suitable for EF Core tracking.</returns>
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
