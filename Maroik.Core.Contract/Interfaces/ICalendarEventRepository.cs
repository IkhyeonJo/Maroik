using Maroik.Core.Contract.Domains.Calendar;
namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarEventRepository : IGenericRepository<CalendarEvent>
{
    Task<long> CreateCalendarEventAsync(CalendarEvent calendarEvent, CancellationToken ct = default);
    Task<List<CalendarEvent>> GetByCalendarIdAsync(long calendarId, CancellationToken ct = default);
    Task<CalendarEvent?> FindByIdAsync(long id, CancellationToken ct = default);
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
