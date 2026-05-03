using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarShared = Maroik.Core.PostgreSQL.Models.CalendarShared;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarSharedRepository"/>.</summary>
public class CalendarSharedRepository(ApplicationDbContext context) : ICalendarSharedRepository
{
    /// <inheritdoc />
    public async Task<List<CalendarShared>> GetCalendarSharedAsync()
        => (await context.CalendarShareds.OrderBy(x => x.CalendarId).ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task CreateCalendarSharedAsync(CalendarShared shared)
    {
        _ = await context.CalendarShareds.AddAsync(ToEntity(shared));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCalendarSharedAsync(CalendarShared shared)
    {
        _ = context.CalendarShareds.Update(ToEntity(shared));
        _ = await context.SaveChangesAsync();
    }

    private static CalendarShared ToDomain(OrmCalendarShared e)
        => CalendarShared.Reconstitute(e.CalendarId, e.User, e.Anonymous);

    private static OrmCalendarShared ToEntity(CalendarShared s) => new()
    {
        CalendarId = s.Id,
        User = s.User,
        Anonymous = s.Anonymous
    };
}
