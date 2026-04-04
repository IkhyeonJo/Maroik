using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarRepository
{
    public Task<List<CalendarResponse>> GetAllCalendarsAsync();
    public Task<List<CalendarResponse>> GetCalendarsAsync(string email);
    public Task CreateCalendarAsync(CalendarRequest request);
    public Task UpdateCalendarAsync(CalendarRequest request);
    public Task DeleteCalendarAsync(CalendarRequest request);
}
