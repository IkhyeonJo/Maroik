using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarEventAttachedFileRepository
{
    public Task<List<CalendarEventAttachedFileResponse>> GetAllAsync();
    public Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId);
    public Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFileRequest request);
    public Task UpdateCalendarEventAttachedFileAsync(CalendarEventAttachedFileRequest request);
    public Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId);
}
