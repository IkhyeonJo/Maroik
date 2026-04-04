using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarSharedRepository
{
    public Task<List<CalendarSharedResponse>> GetCalendarSharedAsync();
    public Task CreateCalendarSharedAsync(CalendarSharedRequest request);
    public Task UpdateCalendarSharedAsync(CalendarSharedRequest request);
}
