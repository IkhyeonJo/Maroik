using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendar = Maroik.Core.PostgreSQL.Models.Calendar;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarRepository"/>.</summary>
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
        _ = context.Calendars.Remove(ToEntity(calendar));
        _ = await context.SaveChangesAsync();
    }

    private static Calendar ToDomain(OrmCalendar e) => Calendar.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.Name ?? string.Empty,
        e.Description, e.TimeZoneIanaId ?? "UTC", e.HtmlColorCode ?? string.Empty,
        e.Created, e.Updated);

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
