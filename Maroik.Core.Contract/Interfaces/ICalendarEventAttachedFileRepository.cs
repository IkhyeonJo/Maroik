using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Low-level repository for the CalendarEventAttachedFile table.
/// Each calendar event may have at most one attached file.
/// </summary>
public interface ICalendarEventAttachedFileRepository
{
    /// <summary>Returns all attached-file records (admin / migration use).</summary>
    public Task<List<CalendarEventAttachedFileResponse>> GetAllAsync();

    /// <summary>Returns the file attached to the given calendar event, or null.</summary>
    public Task<CalendarEventAttachedFileResponse?> GetCalendarEventAttachedFileAsync(long calendarEventId);

    /// <summary>Inserts a new attached-file record for a calendar event.</summary>
    public Task SaveCalendarEventAttachedFileAsync(CalendarEventAttachedFileRequest request);

    /// <summary>Updates the metadata of an existing attached-file record.</summary>
    public Task UpdateCalendarEventAttachedFileAsync(CalendarEventAttachedFileRequest request);

    /// <summary>Deletes the attached-file record for the given calendar event. Returns the number of rows deleted.</summary>
    public Task<int> DeleteCalendarEventAttachedFileAsync(long calendarEventId);
}
