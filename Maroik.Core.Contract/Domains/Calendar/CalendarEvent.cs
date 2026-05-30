using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Calendar;

/// <summary>
/// Aggregate root for a calendar event.
/// Owns its reminders and attached files as child entities.
/// References the parent <see cref="Calendar"/> by <see cref="CalendarId"/> (no object reference).
/// </summary>
public sealed class CalendarEvent : AggregateRoot<long>
{
    private readonly List<CalendarEventReminder> _reminders = [];
    private readonly List<CalendarEventAttachedFile> _attachedFiles = [];

    /// <summary>ID of the calendar this event belongs to.</summary>
    public long CalendarId { get; private set; }

    /// <summary>Event title displayed on the calendar.</summary>
    public string Title { get; private set; }

    /// <summary>Optional rich-text description.</summary>
    public string? Description { get; private set; }

    /// <summary>When true, the event spans the entire day (no specific start/end times).</summary>
    public bool AllDay { get; private set; }

    /// <summary>Event start date/time stored in UTC.</summary>
    public DateTime StartDate { get; private set; }

    /// <summary>Event end date/time stored in UTC.</summary>
    public DateTime EndDate { get; private set; }

    /// <summary>IANA time-zone ID of the start date. Null means UTC.</summary>
    public string? StartDateTimeZoneIanaId { get; private set; }

    /// <summary>IANA time-zone ID of the end date. Null means UTC.</summary>
    public string? EndDateTimeZoneIanaId { get; private set; }

    /// <summary>Physical or virtual location of the event.</summary>
    public string? Location { get; private set; }

    /// <summary>Availability status shown to subscribers (e.g. "Busy", "Free").</summary>
    public string? Status { get; private set; }

    /// <summary>Optional reference to a recurrence rule row.</summary>
    public long? RecurrenceId { get; private set; }

    /// <summary>UTC timestamp when the event was created.</summary>
    public DateTime Created { get; private set; }

    /// <summary>UTC timestamp of the most recent update.</summary>
    public DateTime Updated { get; private set; }

    /// <summary>Read-only view of the reminders owned by this event.</summary>
    public IReadOnlyList<CalendarEventReminder> Reminders => _reminders.AsReadOnly();

    /// <summary>Read-only view of the files attached to this event.</summary>
    public IReadOnlyList<CalendarEventAttachedFile> AttachedFiles => _attachedFiles.AsReadOnly();

    private CalendarEvent(
        long id,
        long calendarId,
        string title,
        string? description,
        bool allDay,
        DateTime startDate,
        DateTime endDate,
        string? startTz,
        string? endTz,
        string? location,
        string? status,
        long? recurrenceId) : base(id)
    {
        CalendarId = calendarId;
        Title = title;
        Description = description;
        AllDay = allDay;
        StartDate = startDate;
        EndDate = endDate;
        StartDateTimeZoneIanaId = startTz;
        EndDateTimeZoneIanaId = endTz;
        Location = location;
        Status = status;
        RecurrenceId = recurrenceId;
        Created = DateTime.UtcNow;
        Updated = DateTime.UtcNow;
    }

    // ------------------------------------------------------------------------
    // Factory / Reconstitution
    // ------------------------------------------------------------------------

    /// <summary>
    /// Rebuilds a <see cref="CalendarEvent"/> from trusted raw values from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// Does NOT raise domain events. Child reminders and files must be loaded separately.
    /// </summary>
    public static CalendarEvent Reconstitute(
        long id,
        long calendarId,
        string title,
        string? description,
        bool allDay,
        DateTime startDate,
        DateTime endDate,
        string? startTz,
        string? endTz,
        string? location,
        string? status,
        long? recurrenceId,
        DateTime created,
        DateTime updated)
    {
        var calendarEvent = new CalendarEvent(
            id, calendarId, title, description, allDay,
            startDate, endDate, startTz, endTz, location, status, recurrenceId);

        calendarEvent.Created = created;
        calendarEvent.Updated = updated;
        return calendarEvent;
    }

    /// <summary>
    /// Creates a new calendar event and raises <see cref="CalendarEventAddedEvent"/>.
    /// </summary>
    public static ErrorOr<CalendarEvent> Create(
        long calendarId,
        string? title,
        string? description,
        bool allDay,
        DateTime startDate,
        DateTime endDate,
        string? startTz,
        string? endTz,
        string? location,
        string? status,
        long? recurrenceId = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Error.Validation("CalendarEvent.TitleEmpty", "Event title cannot be empty.");

        if (title.Length > 100)
            return Error.Validation("CalendarEvent.TitleTooLong", "Event title must be 100 characters or fewer.");

        if (description?.Length > 16384)
            return Error.Validation("CalendarEvent.DescriptionTooLong", "Event description must be 16384 characters or fewer.");

        if (!allDay && endDate < startDate)
            return Error.Validation("CalendarEvent.InvalidDateRange", "End date must not be earlier than start date.");

        var calendarEvent = new CalendarEvent(0, calendarId, title, description, allDay,
            startDate, endDate, startTz, endTz, location, status, recurrenceId);

        return calendarEvent;
    }

    // ------------------------------------------------------------------------
    // Domain behaviours
    // ------------------------------------------------------------------------

    /// <summary>Re-assigns the event to a different calendar or changes its recurrence rule.</summary>
    public void Reassign(long calendarId, long? recurrenceId)
    {
        CalendarId = calendarId;
        RecurrenceId = recurrenceId;
        Updated = DateTime.UtcNow;
    }

    /// <summary>Updates the event's editable fields.</summary>
    public ErrorOr<Success> Update(
        string? title,
        string? description,
        bool allDay,
        DateTime startDate,
        DateTime endDate,
        string? startTz,
        string? endTz,
        string? location,
        string? status)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Error.Validation("CalendarEvent.TitleEmpty", "Event title cannot be empty.");

        if (title.Length > 100)
            return Error.Validation("CalendarEvent.TitleTooLong", "Event title must be 100 characters or fewer.");

        if (description?.Length > 16384)
            return Error.Validation("CalendarEvent.DescriptionTooLong", "Event description must be 16384 characters or fewer.");

        if (!allDay && endDate < startDate)
            return Error.Validation("CalendarEvent.InvalidDateRange", "End date must not be earlier than start date.");

        Title = title;
        Description = description;
        AllDay = allDay;
        StartDate = startDate;
        EndDate = endDate;
        StartDateTimeZoneIanaId = startTz;
        EndDateTimeZoneIanaId = endTz;
        Location = location;
        Status = status;
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Adds a reminder to this event.</summary>
    public ErrorOr<Success> AddReminder(CalendarEventReminder reminder)
    {
        if (reminder.CalendarEventId != Id)
            return Error.Validation("CalendarEvent.ReminderMismatch", "Reminder does not belong to this event.");

        _reminders.Add(reminder);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Removes the reminder with the given ID from this event.</summary>
    public ErrorOr<Success> RemoveReminder(long reminderId)
    {
        var reminder = _reminders.Find(r => r.Id == reminderId);
        if (reminder is null)
            return Error.NotFound("CalendarEvent.ReminderNotFound", $"Reminder {reminderId} not found on this event.");

        _reminders.Remove(reminder);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Attaches a file to this event.</summary>
    public ErrorOr<Success> AttachFile(CalendarEventAttachedFile file)
    {
        if (file.CalendarEventId != Id)
            return Error.Validation("CalendarEvent.FileMismatch", "Attached file does not belong to this event.");

        _attachedFiles.Add(file);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }

    /// <summary>Removes the attached file with the given ID.</summary>
    public ErrorOr<Success> RemoveFile(long fileId)
    {
        var file = _attachedFiles.Find(f => f.Id == fileId);
        if (file is null)
            return Error.NotFound("CalendarEvent.FileNotFound", $"Attached file {fileId} not found on this event.");

        _attachedFiles.Remove(file);
        Updated = DateTime.UtcNow;
        return Result.Success;
    }
}
