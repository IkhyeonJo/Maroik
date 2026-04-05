using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

public interface ICalendarService
{
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
