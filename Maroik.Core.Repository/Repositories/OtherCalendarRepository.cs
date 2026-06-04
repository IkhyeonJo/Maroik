using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmOtherCalendar = Maroik.Core.PostgreSQL.Models.OtherCalendar;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="OtherCalendar"/> domain objects,
/// which link an account to calendars they have subscribed to but do not own.
/// </summary>
public class OtherCalendarRepository(ApplicationDbContext context)
    : GenericRepository<OtherCalendar, OrmOtherCalendar>(context), IOtherCalendarRepository
{
    protected override DbSet<OrmOtherCalendar> Set => Context.OtherCalendars;

    protected override OtherCalendar ToDomain(OrmOtherCalendar e)
        => OtherCalendar.Reconstitute(e.AccountEmail ?? string.Empty, e.CalendarId);

    protected override OrmOtherCalendar ToEntity(OtherCalendar o) => new()
    {
        AccountEmail = o.AccountEmail.Value,
        CalendarId = o.CalendarId
    };

    public Task<List<OtherCalendar>> GetByAccountEmailAsync(string email, CancellationToken ct = default)
        => QueryAsync(e => e.AccountEmail == email, ct: ct);

    public Task<List<OtherCalendar>> GetAllAsync(CancellationToken ct = default)
        => QueryAllAsync(ct: ct);

    public Task DeleteByAccountEmailAsync(string email, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.AccountEmail == email, ct);

    public Task DeleteByAccountEmailAndCalendarIdAsync(string email, long calendarId, CancellationToken ct = default)
        => DeleteWhereAsync(e => e.AccountEmail == email && e.CalendarId == calendarId, ct);
}
