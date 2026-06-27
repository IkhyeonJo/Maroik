using Maroik.Core.Contract.Domains.Calendar;
namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarEventAttachedFileRepository : IGenericRepository<CalendarEventAttachedFile>
{
    Task<CalendarEventAttachedFile?> FindByCalendarEventIdAsync(long calendarEventId, CancellationToken ct = default);
    Task DeleteByCalendarEventIdAsync(long calendarEventId, CancellationToken ct = default);
}
