using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarShared = Maroik.Core.PostgreSQL.Models.CalendarShared;

namespace Maroik.Core.Repository.Repositories;

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
