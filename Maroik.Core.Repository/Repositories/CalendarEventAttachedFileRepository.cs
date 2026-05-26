using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFile = Maroik.Core.PostgreSQL.Models.CalendarEventAttachedFile;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core implementation of <see cref="ICalendarEventAttachedFileRepository"/> that persists and
/// retrieves <see cref="CalendarEventAttachedFile"/> domain objects via <see cref="ApplicationDbContext"/>.
/// Uses a primary constructor to receive the context from the DI container.
/// </summary>
public class CalendarEventAttachedFileRepository(ApplicationDbContext context) : ICalendarEventAttachedFileRepository
{
    /// <inheritdoc />
    public async Task<List<CalendarEventAttachedFile>> GetAllAsync()
        => (await context.CalendarEventAttachedFiles.ToListAsync()).Select(ToDomain).ToList();

    /// <inheritdoc />
    public async Task<CalendarEventAttachedFile?> GetCalendarEventAttachedFileAsync(long calendarEventId)
    {
        OrmFile? file = await context.CalendarEventAttachedFiles
            .Where(x => x.CalendarEventId == calendarEventId)
            .FirstOrDefaultAsync();
        return file == null ? null : ToDomain(file);
    }

    /// <inheritdoc />
    public async Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFile file)
    {
        _ = await context.CalendarEventAttachedFiles.AddAsync(ToEntity(file));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task UpdateCalendarEventAttachedFileAsync(CalendarEventAttachedFile file)
    {
        var existing = await context.CalendarEventAttachedFiles.FindAsync(file.Id);
        if (existing == null) return;
        context.Entry(existing).CurrentValues.SetValues(ToEntity(file));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId)
    {
        var ids = await context.CalendarEventAttachedFiles
            .Where(x => x.CalendarEventId == calendarEventId)
            .Select(x => x.Id)
            .ToListAsync();
        foreach (var id in ids)
        {
            var file = await context.CalendarEventAttachedFiles.FindAsync(id);
            if (file != null)
                context.CalendarEventAttachedFiles.Remove(file);
        }
        return await context.SaveChangesAsync();
    }

    /// <summary>
    /// Maps an ORM <see cref="OrmFile"/> row to the <see cref="CalendarEventAttachedFile"/> domain object.
    /// </summary>
    /// <param name="e">The ORM entity loaded from the database.</param>
    /// <returns>A fully reconstituted <see cref="CalendarEventAttachedFile"/> domain object.</returns>
    private static CalendarEventAttachedFile ToDomain(OrmFile e)
        => CalendarEventAttachedFile.Reconstitute(e.Id, e.CalendarEventId, e.Size, e.Name, e.Extension, e.Path);

    /// <summary>
    /// Maps a <see cref="CalendarEventAttachedFile"/> domain object to an ORM <see cref="OrmFile"/> entity
    /// ready for insertion or update.
    /// </summary>
    /// <param name="f">The domain object to convert.</param>
    /// <returns>An <see cref="OrmFile"/> entity suitable for EF Core tracking.</returns>
    private static OrmFile ToEntity(CalendarEventAttachedFile f) => new()
    {
        Id = f.Id,
        CalendarEventId = f.CalendarEventId,
        Size = f.Size,
        Name = f.Name,
        Extension = f.Extension,
        Path = f.Path
    };
}
