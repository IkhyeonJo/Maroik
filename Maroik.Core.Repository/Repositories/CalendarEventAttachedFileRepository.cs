using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFile = Maroik.Core.PostgreSQL.Models.CalendarEventAttachedFile;

namespace Maroik.Core.Repository.Repositories;

/// <summary>EF Core implementation of <see cref="ICalendarEventAttachedFileRepository"/>.</summary>
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
        _ = context.CalendarEventAttachedFiles.Update(ToEntity(file));
        _ = await context.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId)
        => await context.Database.ExecuteSqlInterpolatedAsync(
            $"""DELETE FROM "CalendarEventAttachedFile" WHERE "CalendarEventId" = {calendarEventId}""");

    private static CalendarEventAttachedFile ToDomain(OrmFile e)
        => CalendarEventAttachedFile.Reconstitute(e.Id, e.CalendarEventId, e.Size, e.Name, e.Extension, e.Path);

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
