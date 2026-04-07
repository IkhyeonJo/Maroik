namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object used to create a reminder for a calendar event.
/// Exactly one of the "BeforeEvent" fields should be set to indicate the lead time.
/// </summary>
public class CalendarEventReminderRequest
{
    /// <summary>Reminder ID (auto-incremented primary key; 0 for new reminders).</summary>
    public long Id { get; set; }

    /// <summary>ID of the calendar event this reminder belongs to (foreign key).</summary>
    public long CalendarEventId { get; set; }

    /// <summary>Notification delivery method (e.g. "Email", "Push").</summary>
    public string? Method { get; set; }

    /// <summary>How many minutes before the event to send the reminder. Null if another unit is used.</summary>
    public long? MinutesBeforeEvent { get; set; }

    /// <summary>How many hours before the event to send the reminder. Null if another unit is used.</summary>
    public long? HoursBeforeEvent { get; set; }

    /// <summary>How many days before the event to send the reminder. Null if another unit is used.</summary>
    public long? DaysBeforeEvent { get; set; }

    /// <summary>How many weeks before the event to send the reminder. Null if another unit is used.</summary>
    public long? WeeksBeforeEvent { get; set; }

    /// <summary>Specific time-of-day at which the reminder fires on the target day.</summary>
    public TimeOnly? TimesBeforeEvent { get; set; }
}
