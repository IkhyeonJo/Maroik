using Maroik.Core.Contract.Domains.Calendar;
namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarRepository : IGenericRepository<Calendar>
{
    Task<List<Calendar>> GetByAccountEmailAsync(string accountEmail, CancellationToken ct = default);
    Task<List<Calendar>> GetAllOrderedByNameAsync(CancellationToken ct = default);
    Task DeleteByIdAsync(long id, CancellationToken ct = default);
}
