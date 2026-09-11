// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Lightweight reminder descriptor passed from the view/controller to the service layer
/// when creating or updating calendar events.
/// </summary>
public class CalendarReminderDto
{
    /// <summary>Notification delivery method — see <c>Maroik.Core.Domain.Calendar.ReminderMethods</c> for the allowed values.</summary>
    public string Method { get; init; } = "";

    /// <summary>Minutes before the event to fire the reminder. Null if another unit is used.</summary>
    public int? MinutesBeforeEvent { get; init; }

    /// <summary>Hours before the event to fire the reminder. Null if another unit is used.</summary>
    public int? HoursBeforeEvent { get; init; }

    /// <summary>Days before the event to fire the reminder. Null if another unit is used.</summary>
    public int? DaysBeforeEvent { get; init; }

    /// <summary>Weeks before the event to fire the reminder. Null if another unit is used.</summary>
    public int? WeeksBeforeEvent { get; init; }

    /// <summary>Specific time-of-day at which the reminder fires on the target day.</summary>
    public TimeSpan? TimesBeforeEvent { get; init; }
}
