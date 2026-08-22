using ErrorOr;
using Maroik.Core.Domain.Primitives;

namespace Maroik.Core.Domain.Calendar;

/// <summary>
/// Entity representing a reminder attached to a <see cref="CalendarEvent"/>.
/// Exactly one "BeforeEvent" field should carry a non-null value to define the lead time.
/// Owned by the <see cref="CalendarEvent"/> aggregate root.
/// </summary>
public sealed class CalendarEventReminder : Entity<long>
{
    /// <summary>ID of the parent calendar event.</summary>
    public long CalendarEventId { get; private set; }

    /// <summary>Notification delivery method (e.g. "Email", "Push").</summary>
    public string Method { get; private set; }

    /// <summary>Minutes before the event to send the reminder.</summary>
    public long? MinutesBeforeEvent { get; private set; }

    /// <summary>Hours before the event to send the reminder.</summary>
    public long? HoursBeforeEvent { get; private set; }

    /// <summary>Days before the event to send the reminder.</summary>
    public long? DaysBeforeEvent { get; private set; }

    /// <summary>Weeks before the event to send the reminder.</summary>
    public long? WeeksBeforeEvent { get; private set; }

    /// <summary>Time-of-day at which the reminder fires on the target day.</summary>
    public TimeOnly? TimesBeforeEvent { get; private set; }

    private CalendarEventReminder(
        long id,
        long calendarEventId,
        string method,
        long? minutesBefore,
        long? hoursBefore,
        long? daysBefore,
        long? weeksBefore,
        TimeOnly? timesBefore) : base(id)
    {
        CalendarEventId = calendarEventId;
        Method = method;
        MinutesBeforeEvent = minutesBefore;
        HoursBeforeEvent = hoursBefore;
        DaysBeforeEvent = daysBefore;
        WeeksBeforeEvent = weeksBefore;
        TimesBeforeEvent = timesBefore;
    }

    /// <summary>
    /// Rebuilds a <see cref="CalendarEventReminder"/> from trusted data from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// </summary>
    public static CalendarEventReminder Reconstitute(
        long id, long calendarEventId, string? method,
        long? minutesBefore, long? hoursBefore, long? daysBefore, long? weeksBefore, TimeOnly? timesBefore)
        => new(id, calendarEventId, method ?? "", minutesBefore, hoursBefore, daysBefore, weeksBefore, timesBefore);

    /// <summary>
    /// Creates a new reminder.
    /// Exactly one of the "before" parameters must be non-null.
    /// </summary>
    public static ErrorOr<CalendarEventReminder> Create(
        long calendarEventId,
        string? method,
        long? minutesBefore,
        long? hoursBefore,
        long? daysBefore,
        long? weeksBefore,
        TimeOnly? timesBefore)
    {
        if (string.IsNullOrWhiteSpace(method))
            return Error.Validation("Reminder.MethodEmpty", "Notification method cannot be empty.");

        // The client models each reminder as a single unit+value pair (a dropdown picks minutes/
        // hours/days/weeks, then one value field) — multiple simultaneous lead-time fields have no
        // defined meaning, so exactly one must be set.
        int leadTimeFieldCount = (minutesBefore.HasValue ? 1 : 0) + (hoursBefore.HasValue ? 1 : 0)
                                  + (daysBefore.HasValue ? 1 : 0) + (weeksBefore.HasValue ? 1 : 0);

        if (leadTimeFieldCount != 1)
            return Error.Validation("Reminder.InvalidLeadTime", "Exactly one lead-time field (minutes/hours/days/weeks) must be set.");

        return new CalendarEventReminder(0, calendarEventId, method,
            minutesBefore, hoursBefore, daysBefore, weeksBefore, timesBefore);
    }
}
