using Maroik.Core.Contract.Domains.Calendar;
using Maroik.Core.Contract.Interfaces;
using Maroik.Core.PostgreSQL.Data;
using Microsoft.EntityFrameworkCore;
using OrmCalendar = Maroik.Core.PostgreSQL.Models.Calendar;

namespace Maroik.Core.Repository.Repositories;

/// <summary>
/// EF Core repository for <see cref="Calendar"/> domain objects.
/// Maps color codes and IANA time zone IDs between domain value objects and plain strings.
/// </summary>
public class CalendarRepository(ApplicationDbContext context)
    : GenericRepository<Calendar, OrmCalendar>(context), ICalendarRepository
{
    protected override DbSet<OrmCalendar> Set => Context.Calendars;

    protected override Calendar ToDomain(OrmCalendar e) => Calendar.Reconstitute(
        e.Id, e.AccountEmail ?? string.Empty, e.Name ?? string.Empty,
        e.Description, e.TimeZoneIanaId ?? "UTC", e.HtmlColorCode ?? string.Empty,
        e.Created, e.Updated);

    protected override OrmCalendar ToEntity(Calendar c) => new()
    {
        Id = c.Id,
        AccountEmail = c.AccountEmail.Value,
        Name = c.Name,
        Description = c.Description,
        TimeZoneIanaId = c.TimeZone.Value,
        HtmlColorCode = c.ColorCode.Value,
        Created = c.Created,
        Updated = c.Updated
    };
}
