using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmOtherCalendar = Maroik.Core.PostgreSQL.Models.OtherCalendar;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IOtherCalendarRepository"/> that persists and retrieves
/// <see cref="OtherCalendar"/> domain objects via <see cref="ApplicationDbContext"/>.
/// An "other calendar" represents a calendar owned by a different user that this account
/// has been granted access to.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
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
        // Hard-delete: removes the subscription row so the account no longer sees the shared calendar.
        var existing = await context.OtherCalendars.FindAsync(otherCalendar.AccountEmail.Value, otherCalendar.CalendarId);
        if (existing == null) return;
        _ = context.OtherCalendars.Remove(existing);
        _ = await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmOtherCalendar"/> row to the <see cref="OtherCalendar"/> domain object,
    /// applying a null-coalescing default for the required AccountEmail field.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="OtherCalendar"/> domain object.</returns>
    private static OtherCalendar ToDomain(OrmOtherCalendar e)
        => OtherCalendar.Reconstitute(e.AccountEmail ?? string.Empty, e.CalendarId);

    /// <summary>
    /// Maps an <see cref="OtherCalendar"/> domain object to an ORM <see cref="OrmOtherCalendar"/> entity
    /// ready for insertion or deletion.
    /// </summary>
    /// <param name="o">The domain object to convert.</param>
    /// <returns>An <see cref="OrmOtherCalendar"/> entity suitable for EF Core tracking.</returns>
    private static OrmOtherCalendar ToEntity(OtherCalendar o) => new()
    {
        AccountEmail = o.AccountEmail.Value,
        CalendarId = o.CalendarId
    };
}
