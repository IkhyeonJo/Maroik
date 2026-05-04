using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendarShared = Maroik.Core.PostgreSQL.Models.CalendarShared;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICalendarSharedRepository"/> that persists and retrieves
/// <see cref="CalendarShared"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
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

    /// <summary>
    /// Maps an ORM <see cref="OrmCalendarShared"/> row to the <see cref="CalendarShared"/> domain object.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="CalendarShared"/> domain object.</returns>
    private static CalendarShared ToDomain(OrmCalendarShared e)
        => CalendarShared.Reconstitute(e.CalendarId, e.User, e.Anonymous);

    /// <summary>
    /// Maps a <see cref="CalendarShared"/> domain object to an ORM <see cref="OrmCalendarShared"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="s">The domain object to convert.</param>
    /// <returns>An <see cref="OrmCalendarShared"/> entity suitable for EF Core tracking.</returns>
    private static OrmCalendarShared ToEntity(CalendarShared s) => new()
    {
        CalendarId = s.Id,
        User = s.User,
        Anonymous = s.Anonymous
    };
}
