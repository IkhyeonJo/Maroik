using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmOtherCalendar = Maroik.Core.PostgreSQL.Models.OtherCalendar;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="IOtherCalendarRepository"/>.</summary>
public class OtherCalendarRepository(ApplicationDbContext context) : IOtherCalendarRepository
{
    /// <inheritdoc />
    public async Task<List<OtherCalendar>> GetAllOtherCalendarsAsync()
        => (await context.OtherCalendars.OrderBy(x => x.CalendarId).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<List<OtherCalendar>> GetOtherCalendarsAsync(string email)
        => (await context.OtherCalendars.Where(x => x.AccountEmail == email).OrderBy(x => x.CalendarId).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task CreateOtherCalendarAsync(OtherCalendar otherCalendar)
    {
        _ = await context.OtherCalendars.AddAsync(ToEntity(otherCalendar));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task DeleteOtherCalendarAsync(OtherCalendar otherCalendar)
    {
        _ = context.OtherCalendars.Remove(ToEntity(otherCalendar));
        _ = await context.SaveChangesAsync();
    }

    private static OtherCalendar ToDomain(OrmOtherCalendar e)
        => OtherCalendar.Reconstitute(e.AccountEmail ?? string.Empty, e.CalendarId);

    private static OrmOtherCalendar ToEntity(OtherCalendar o) => new()
    {
        AccountEmail = o.AccountEmail.Value,
        CalendarId = o.CalendarId
    };
}
