using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarShared = Maroik.Core.PostgreSQL.Models.CalendarShared;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="CalendarShared"/> domain objects,
/// which control whether a calendar is visible to authenticated users or anonymous visitors.
/// </summary>
public class CalendarSharedRepository(ApplicationDbContext context)
    : GenericRepository<CalendarShared, OrmCalendarShared>(context), ICalendarSharedRepository
{
    protected override DbSet<OrmCalendarShared> Set => Context.CalendarShareds;

    protected override CalendarShared ToDomain(OrmCalendarShared e)
        => CalendarShared.Reconstitute(e.CalendarId, e.User, e.Anonymous);

    protected override OrmCalendarShared ToEntity(CalendarShared s) => new()
    {
        CalendarId = s.Id,
        User = s.User,
        Anonymous = s.Anonymous
    };
}
