using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarService
{
    Task<List<CalendarResponse>> GetCalendarsAsync(string email);
    Task<List<CalendarResponse>> GetAllCalendarsAsync();
    Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId);
    Task<CalendarEventResponse?> GetCalendarEventAsync(long id);
    Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId);
    Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId);
    Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email);
    Task<List<CalendarSharedResponse>> GetCalendarSharedAsync();
    Task<ServiceResult> EnsureCalendarSharedAsync(long calendarId);
    Task<ServiceResult> CreateCalendarAsync(CalendarRequest request);
    Task<ServiceResult> UpdateCalendarAsync(CalendarRequest request);
    Task<ServiceResult> DeleteCalendarAsync(CalendarRequest request);
    Task<ServiceResult> UpdateOtherCalendarAsync(string email, IEnumerable<OtherCalendarRequest> requests);
    Task<ServiceResult> UpdateCalendarSharedAsync(IEnumerable<CalendarSharedRequest> requests);
    Task<ServiceResult> EnsureDefaultCalendarAsync(CalendarRequest request);
    Task<ServiceResult> CreateCalendarEventAsync(CalendarEventRequest request, string email, string roleIndex, List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile);
    Task<ServiceResult> UpdateCalendarEventAsync(CalendarEventRequest request, string email, string roleIndex, List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile);
    Task<ServiceResult> DeleteCalendarEventAsync(long calendarEventId, string email);
}
