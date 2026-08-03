using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarEvent = Maroik.Core.PostgreSQL.Models.CalendarEvent;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="CalendarEvent"/> domain objects.
/// Returns the database-generated ID when creating a new event.
/// </summary>
public class CalendarEventRepository(ApplicationDbContext context)
    : GenericRepository<CalendarEvent, OrmCalendarEvent>(context), ICalendarEventRepository
{
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmCalendarEvent"/> rows.</summary>
    protected override DbSet<OrmCalendarEvent> Set => Context.CalendarEvents;

    /// <inheritdoc />
    public async Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default)
    {
        OrmCalendarEvent entity = ToEntity(calendarEvent);
        _ = await Context.CalendarEvents.AddAsync(entity, ct);
        _ = await Context.SaveChangesAsync(ct);
        return entity.Id;
    }

    /// <summary>Maps a persisted <see cref="OrmCalendarEvent"/> row to the <see cref="CalendarEvent"/> domain object.</summary>
    protected override CalendarEvent ToDomain(OrmCalendarEvent e) => CalendarEvent.Reconstitute(
        e.Id, e.CalendarId, e.Title ?? "", e.Description,
        e.AllDay, e.StartDate, e.EndDate,
        e.StartDateTimeZoneIanaId, e.EndDateTimeZoneIanaId,
        e.Location, e.Status, e.RecurrenceId, e.Created, e.Updated);

    /// <summary>Maps a <see cref="CalendarEvent"/> domain object to its <see cref="OrmCalendarEvent"/> persistence representation.</summary>
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

    /// <inheritdoc />
    public Task<List<CalendarEvent>> GetByCalendarIdAsync(long calendarId, CancellationToken ct = default)
        => QueryAsync(e => e.CalendarId == calendarId, ct: ct);

    /// <inheritdoc />
    public Task<List<CalendarEvent>> GetByCalendarIdsAsync(IEnumerable<long> calendarIds, CancellationToken ct = default)
    {
        var ids = calendarIds.ToList();
        return QueryAsync(e => ids.Contains(e.CalendarId), ct: ct);
    }

    /// <inheritdoc />
    public Task<CalendarEvent?> FindByIdAsync(long id, CancellationToken ct = default)
        => QueryFirstAsync(e => e.Id == id, ct);

    /// <inheritdoc />
    public Task DeleteByIdAsync(long id, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.Id == id, ct);
}
