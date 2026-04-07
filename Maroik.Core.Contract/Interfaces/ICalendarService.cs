using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for all calendar-related business logic, including
/// calendar CRUD, event management, sharing, subscriptions, and file uploads.
/// </summary>
public interface ICalendarService
{
    /// <summary>Returns calendars owned by the given account.</summary>
    Task<List<CalendarResponse>> GetCalendarsAsync(string email);

    /// <summary>Returns every calendar in the system (used for shared-calendar discovery).</summary>
    Task<List<CalendarResponse>> GetAllCalendarsAsync();

    /// <summary>Returns all events belonging to the specified calendar.</summary>
    Task<IEnumerable<CalendarEventResponse>> GetCalendarEventsAsync(long calendarId);

    /// <summary>Returns a single calendar event by ID, or null.</summary>
    Task<CalendarEventResponse?> GetCalendarEventAsync(long id);

    /// <summary>Returns the file attached to the given calendar event, or null.</summary>
    Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId);

    /// <summary>Returns all reminder settings for the given calendar event.</summary>
    Task<IEnumerable<CalendarEventReminderResponse>> GetCalendarEventRemindersAsync(long calendarEventId);

    /// <summary>Returns calendars that the given account has subscribed to from other users.</summary>
    Task<List<OtherCalendarResponse>> GetOtherCalendarsAsync(string email);

    /// <summary>Returns the sharing-permission rows for all calendars.</summary>
    Task<List<CalendarSharedResponse>> GetCalendarSharedAsync();

    /// <summary>Creates the CalendarShared row for a calendar if it does not already exist.</summary>
    Task<ServiceResult> EnsureCalendarSharedAsync(long calendarId);

    /// <summary>Creates a new calendar for the requesting user.</summary>
    Task<ServiceResult> CreateCalendarAsync(CalendarRequest request);

    /// <summary>Updates an existing calendar's metadata.</summary>
    Task<ServiceResult> UpdateCalendarAsync(CalendarRequest request);

    /// <summary>Deletes a calendar and all its events.</summary>
    Task<ServiceResult> DeleteCalendarAsync(CalendarRequest request);

    /// <summary>
    /// Replaces the set of other-user calendars that the account subscribes to
    /// with the provided list (full replace, not a patch).
    /// </summary>
    Task<ServiceResult> UpdateOtherCalendarAsync(string email, IEnumerable<OtherCalendarRequest> requests);

    /// <summary>Bulk-updates the sharing permissions for the provided calendars.</summary>
    Task<ServiceResult> UpdateCalendarSharedAsync(IEnumerable<CalendarSharedRequest> requests);

    /// <summary>Creates a default calendar for a new account if one does not already exist.</summary>
    Task<ServiceResult> EnsureDefaultCalendarAsync(CalendarRequest request);

    /// <summary>Creates a new calendar event with optional reminders and an optional file attachment.</summary>
    Task<ServiceResult> CreateCalendarEventAsync(CalendarEventRequest request, string email, string roleIndex, List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile);

    /// <summary>Updates an existing calendar event, replacing reminders and the attachment if provided.</summary>
    Task<ServiceResult> UpdateCalendarEventAsync(CalendarEventRequest request, string email, string roleIndex, List<CalendarReminderInfo> reminders, AttachedFileInfo? attachedFile);

    /// <summary>Deletes a calendar event and its associated reminders and attachment.</summary>
    Task<ServiceResult> DeleteCalendarEventAsync(long calendarEventId, string email);

    /// <summary>Validates and stores an image uploaded from the Summernote editor inside a calendar event description.</summary>
    Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string roleIndex);

    /// <summary>Downloads the raw bytes of a stored file by its server-side path.</summary>
    Task<byte[]?> DownloadFileAsync(string filePath);
}
