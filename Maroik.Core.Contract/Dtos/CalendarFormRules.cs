namespace Maroik.Core.Contract.Dtos;

/// <summary>The reminder limits the calendar pages mirror; authoritative in the Domain.</summary>
public sealed class CalendarFormRules
{
    /// <summary>The smallest lead time of a reminder.</summary>
    public long MinLeadTimeBeforeEvent { get; init; }

    /// <summary>The largest lead time in minutes.</summary>
    public long MaxMinutesBeforeEvent { get; init; }

    /// <summary>The largest lead time in hours.</summary>
    public long MaxHoursBeforeEvent { get; init; }

    /// <summary>The largest lead time in days.</summary>
    public long MaxDaysBeforeEvent { get; init; }

    /// <summary>The largest lead time in weeks.</summary>
    public long MaxWeeksBeforeEvent { get; init; }

    /// <summary>The step of an all-day reminder's time of day, in minutes.</summary>
    public int ReminderTimeOfDayStepMinutes { get; init; }

    /// <summary>The time of day an all-day reminder starts from.</summary>
    public string DefaultReminderTimeOfDay { get; init; } = "";

    /// <summary>The most reminders one event holds.</summary>
    public int MaxRemindersPerEvent { get; init; }
}
