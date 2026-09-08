using ErrorOr;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.ValueObjects;

namespace Maroik.Core.Domain.Calendar;

/// <summary>
/// Aggregate root for a calendar event.
/// Reminders and attached files are persisted independently via their own repositories
/// (<see cref="CalendarEventReminder"/>, <see cref="CalendarEventAttachedFile"/>) rather than
/// hydrated as part of this aggregate, so this class does not mediate their persistence.
/// References the parent <see cref="Calendar"/> by <see cref="CalendarId"/> (no object reference).
/// </summary>
public sealed class CalendarEvent : AggregateRoot<long>
{
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
    /// Child reminders and files must be loaded separately.
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
            startDate, endDate, startTz, endTz, location, status, recurrenceId) { Created = created, Updated = updated };

        return calendarEvent;
    }

    /// <summary>
    /// Creates a new calendar event.
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

        var timesResult = ValidateDatesAndZones(allDay, startDate, endDate, startTz, endTz);
        if (timesResult.IsError) return timesResult.Errors;

        var calendarEvent = new CalendarEvent(0, calendarId, title, description, allDay,
            startDate, endDate, startTz, endTz, location, status, recurrenceId);

        return calendarEvent;
    }

    /// <summary>
    /// Shared validation for the date range and the optional per-endpoint time-zone IDs, run by
    /// both <see cref="Create"/> and <see cref="Update"/>. The range is checked for an all-day
    /// event too (on the date part) — a "no end before start" rule that only fired for timed
    /// events let an all-day event be saved end-before-start. A non-null start/end zone must
    /// resolve to a known zone on the host (null means UTC).
    /// </summary>
    private static ErrorOr<Success> ValidateDatesAndZones(
        bool allDay, DateTime startDate, DateTime endDate, string? startTz, string? endTz)
    {
        bool rangeInvalid = allDay ? endDate.Date < startDate.Date : endDate < startDate;
        if (rangeInvalid)
            return Error.Validation("CalendarEvent.InvalidDateRange", "End date must not be earlier than start date.");

        foreach (string? tz in new[] { startTz, endTz })
        {
            if (!string.IsNullOrWhiteSpace(tz) && TimeZoneId.Create(tz).IsError)
                return Error.Validation("CalendarEvent.InvalidTimeZone", $"'{tz}' is not a recognised time-zone ID.");
        }

        return Result.Success;
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

        var timesResult = ValidateDatesAndZones(allDay, startDate, endDate, startTz, endTz);
        if (timesResult.IsError) return timesResult.Errors;

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

}
