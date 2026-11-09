using ErrorOr;
using Maroik.Core.Domain.Errors;

namespace Maroik.Core.Domain.Calendars;

/// <summary>
/// Domain rules constraining the lead time of a <see cref="CalendarEventReminder"/>.
/// A reminder fires a fixed amount of time before its event, expressed in exactly one unit
/// (minutes/hours/days/weeks). The maximum lead time is 28 days — the four constants below
/// are that same ceiling restated per unit, so whichever unit the client picked is bounded
/// consistently.
/// Authoritative here; the Calendar page's client-side check is a UX mirror of these values.
/// </summary>
public static class CalendarReminderPolicy
{
    /// <summary>Minimum lead time before the event, in any unit (a reminder at the event's start).</summary>
    public const long MinLeadTimeBeforeEvent = 0;

    /// <summary>Maximum minutes before the event (28 days).</summary>
    public const long MaxMinutesBeforeEvent = 40_320;

    /// <summary>Maximum hours before the event (28 days).</summary>
    public const long MaxHoursBeforeEvent = 672;

    /// <summary>Maximum days before the event (4 weeks).</summary>
    public const long MaxDaysBeforeEvent = 28;

    /// <summary>Maximum weeks before the event.</summary>
    public const long MaxWeeksBeforeEvent = 4;

    /// <summary>
    /// Granularity, in minutes, of the "notify at HH:MM" picker for all-day reminders
    /// (the client builds a 00:00..23:45 list from this).
    /// </summary>
    public const int ReminderTimeOfDayStepMinutes = 15;

    /// <summary>Default "notify at" time-of-day pre-selected for a new all-day reminder.</summary>
    public const string DefaultReminderTimeOfDay = "09:00";

    /// <summary>
    /// The most reminders an event holds. Each reminder is an e-mail through the site's SMTP relay, so an unbounded list let one
    /// request queue tens of thousands of mails and use up the relay's daily quota, and with it everyone's confirmation and reset mails.
    /// </summary>
    public const int MaxRemindersPerEvent = 10;

    /// <summary>Whether an event may hold <paramref name="count"/> reminders (<see cref="MaxRemindersPerEvent"/>).</summary>
    public static ErrorOr<Success> CheckCount(int count) =>
        count > MaxRemindersPerEvent
            ? DomainError.Validation("Reminder.TooMany", "An event can have at most {0} reminders.", MaxRemindersPerEvent)
            : Result.Success;

    /// <summary>
    /// What an all-day event's reminder must look like, the same choice its form offers: a lead time in days or weeks (the event has no
    /// start time to count minutes or hours from), at a time of day on the <see cref="ReminderTimeOfDayStepMinutes"/> step.
    /// </summary>
    public static ErrorOr<Success> CheckAllDayReminder(long? minutesBefore, long? hoursBefore, TimeOnly? timeOfDay)
    {
        if (minutesBefore.HasValue || hoursBefore.HasValue)
            return DomainError.Validation("Reminder.AllDayLeadTimeUnit", "An all-day event's reminder must be set in days or weeks.");

        if (timeOfDay is not { Second: 0, Millisecond: 0, Microsecond: 0, Nanosecond: 0 } time || time.Minute % ReminderTimeOfDayStepMinutes != 0)
            return DomainError.Validation("Reminder.AllDayTimeOfDay", "An all-day event's reminder time must be on a {0}-minute step.", ReminderTimeOfDayStepMinutes);

        return Result.Success;
    }

    /// <summary>
    /// Returns <see langword="true"/> when the single non-null lead-time value falls within
    /// <see cref="MinLeadTimeBeforeEvent"/>..its per-unit maximum (inclusive). Returns <see langword="true"/> when no
    /// lead-time field is set — the "exactly one must be set" rule is enforced separately by
    /// <see cref="CalendarEventReminder.Create"/>.
    /// </summary>
    public static bool IsLeadTimeWithinRange(
        long? minutesBefore, long? hoursBefore, long? daysBefore, long? weeksBefore)
    {
        if (minutesBefore is { } minutes) return minutes is >= MinLeadTimeBeforeEvent and <= MaxMinutesBeforeEvent;
        if (hoursBefore is { } hours) return hours is >= MinLeadTimeBeforeEvent and <= MaxHoursBeforeEvent;
        if (daysBefore is { } days) return days is >= MinLeadTimeBeforeEvent and <= MaxDaysBeforeEvent;
        if (weeksBefore is { } weeks) return weeks is >= MinLeadTimeBeforeEvent and <= MaxWeeksBeforeEvent;
        return true;
    }
}
