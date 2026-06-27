using Maroik.Core.Contract.Domains.Calendar;
namespace Maroik.Core.Contract.Interfaces;

public interface IOtherCalendarRepository : IGenericRepository<OtherCalendar>
{
    Task<List<OtherCalendar>> GetByAccountEmailAsync(string email, CancellationToken ct = default);
    Task<List<OtherCalendar>> GetAllAsync(CancellationToken ct = default);
    Task DeleteByAccountEmailAsync(string email, CancellationToken ct = default);
    Task DeleteByAccountEmailAndCalendarIdAsync(string email, long calendarId, CancellationToken ct = default);
}
