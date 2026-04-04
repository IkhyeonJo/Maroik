using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarEventRepository
{
    public Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId);
    public Task<CalendarEventResponse?> GetCalendarEventAsync(long id);
    public Task CreateCalendarEventAsync(CalendarEventRequest request);
    public Task UpdateCalendarEventAsync(CalendarEventRequest request);
    public Task DeleteCalendarEventAsync(CalendarEventRequest request);
}
