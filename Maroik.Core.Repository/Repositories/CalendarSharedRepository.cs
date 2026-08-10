using Maroik.Core.Domain.Calendar;
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
    /// <summary>The underlying <see cref="DbSet{TEntity}"/> for <see cref="OrmCalendarShared"/> rows.</summary>
    protected override DbSet<OrmCalendarShared> Set => Context.CalendarShareds;

    /// <summary>Maps a persisted <see cref="OrmCalendarShared"/> row to the <see cref="CalendarShared"/> domain object.</summary>
    protected override CalendarShared ToDomain(OrmCalendarShared e)
        => CalendarShared.Reconstitute(e.CalendarId, e.User, e.Anonymous);

    /// <summary>Maps a <see cref="CalendarShared"/> domain object to its <see cref="OrmCalendarShared"/> persistence representation.</summary>
    protected override OrmCalendarShared ToEntity(CalendarShared s) => new()
    {
        CalendarId = s.Id,
        User = s.User,
        Anonymous = s.Anonymous
    };

    /// <inheritdoc />
    public Task<List<CalendarShared>> GetAllAsync(CancellationToken ct = default)
        => QueryAllAsync(ct: ct);
}
