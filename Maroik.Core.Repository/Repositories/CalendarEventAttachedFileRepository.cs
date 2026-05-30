using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmFile = Maroik.Core.PostgreSQL.Models.CalendarEventAttachedFile;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="CalendarEventAttachedFile"/> domain objects.
/// </summary>
public class CalendarEventAttachedFileRepository(ApplicationDbContext context)
    : GenericRepository<CalendarEventAttachedFile, OrmFile>(context), ICalendarEventAttachedFileRepository
{
    protected override DbSet<OrmFile> Set => Context.CalendarEventAttachedFiles;

    protected override CalendarEventAttachedFile ToDomain(OrmFile e)
        => CalendarEventAttachedFile.Reconstitute(e.Id, e.CalendarEventId, e.Size, e.Name, e.Extension, e.Path);

    protected override OrmFile ToEntity(CalendarEventAttachedFile f) => new()
    {
        Id = f.Id,
        CalendarEventId = f.CalendarEventId,
        Size = f.Size,
        Name = f.Name,
        Extension = f.Extension,
        Path = f.Path
    };
}
