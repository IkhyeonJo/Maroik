using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendar = Maroik.Core.PostgreSQL.Models.Calendar;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICalendarRepository"/> that persists and retrieves
/// <see cref="Calendar"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class CalendarRepository(ApplicationDbContext context) : ICalendarRepository
{
    /// <inheritdoc />
    public async Task<List<Calendar>> GetAllCalendarsAsync()
        => (await context.Calendars.OrderBy(x => x.Name).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<List<Calendar>> GetCalendarsAsync(string email)
        => (await context.Calendars.Where(x => x.AccountEmail == email).OrderBy(x => x.Name).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task CreateCalendarAsync(Calendar calendar)
    {
        _ = await context.Calendars.AddAsync(ToEntity(calendar));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCalendarAsync(Calendar calendar)
    {
        _ = context.Calendars.Update(ToEntity(calendar));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteCalendarAsync(Calendar calendar)
    {
        // Hard-delete: the calendar row is physically removed from the database.
        _ = context.Calendars.Remove(ToEntity(calendar));
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmCalendar"/> row to the <see cref="Calendar"/> domain object,
    /// applying null-coalescing defaults for required string fields and a fallback of "UTC"
    /// for the time-zone identifier.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="Calendar"/> domain object.</returns>
    private static Calendar ToDomain(OrmCalendar e) => Calendar.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.Name ?? string.Empty,
        e.Description, e.TimeZoneIanaId ?? "UTC", e.HtmlColorCode ?? string.Empty,
        e.Created, e.Updated);

    /// <summary>
    /// Maps a <see cref="Calendar"/> domain object to an ORM <see cref="OrmCalendar"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="c">The domain object to convert.</param>
    /// <returns>An <see cref="OrmCalendar"/> entity suitable for EF Core tracking.</returns>
    private static OrmCalendar ToEntity(Calendar c) => new()
    {
        Id = c.Id,
        AccountEmail = c.AccountEmail.Value,
        Name = c.Name,
        Description = c.Description,
        TimeZoneIanaId = c.TimeZone.Value,
        HtmlColorCode = c.ColorCode.Value,
        Created = c.Created,
        Updated = c.Updated
    };
}
