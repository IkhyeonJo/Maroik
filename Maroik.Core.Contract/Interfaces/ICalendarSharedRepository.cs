using Maroik.Core.Contract.Domains.Calendar;
namespace Maroik.Core.Contract.Interfaces;
public interface ICalendarSharedRepository : IGenericRepository<CalendarShared>
{
    Task<List<CalendarShared>> GetAllAsync(CancellationToken ct = default);
}
