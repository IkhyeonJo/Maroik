using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Domain.Tests.Calendars;

/// <summary>
/// An all-day event's reminder is what its form offers, whoever sends it: a lead time in days or weeks, at a time of day on the
/// 15-minute step. A timed event's reminder is not held to this.
/// </summary>
public class CalendarAllDayReminderTests
{
    /// <summary>"Now" in these tests.</summary>
    private static readonly DateTime _now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>All day on 2026-11-02.</summary>
    private static readonly CalendarEvent _allDay = CalendarEvent.Reconstitute(2, 7, "Trip", null, true,
        new DateTime(2026, 11, 2), new DateTime(2026, 11, 3), null, null, null, "Busy", null, _now, _now);

    /// <summary>Timed, starting 2026-11-02 09:00 UTC.</summary>
    private static readonly CalendarEvent _timed = CalendarEvent.Reconstitute(1, 7, "Dentist", null, false,
        new DateTime(2026, 11, 2, 9, 0, 0), new DateTime(2026, 11, 2, 10, 0, 0), "UTC", "UTC", null, "Busy", null, _now, _now);

    /// <summary>A lead time in minutes or hours is refused for an all-day event.</summary>
    [Theory]
    [InlineData(30L, null)]
    [InlineData(null, 2L)]
    public void Create_RefusesMinutesOrHours_ForAnAllDayEvent(long? minutes, long? hours)
    {
        var result = CalendarEventReminder.Create(2, _allDay, "UTC", minutes, hours, null, null, new TimeOnly(9, 0), _now);

        Assert.True(result.IsError);
        Assert.Equal("Reminder.AllDayLeadTimeUnit", result.FirstError.Code);
    }

    /// <summary>Days and weeks on the step are accepted, and every quarter of an hour is on it.</summary>
    [Theory]
    [InlineData(1L, null, 0, 0)]
    [InlineData(1L, null, 9, 15)]
    [InlineData(null, 2L, 23, 45)]
    public void Create_AcceptsDaysOrWeeks_OnTheStep(long? days, long? weeks, int hour, int minute)
    {
        var result = CalendarEventReminder.Create(2, _allDay, "UTC", null, null, days, weeks, new TimeOnly(hour, minute), _now);

        Assert.False(result.IsError);
        Assert.Equal(new TimeOnly(hour, minute), result.Value.TimesBeforeEvent);
    }

    /// <summary>A time between two steps, down to its seconds, is refused for an all-day event.</summary>
    [Theory]
    [InlineData(9, 7, 0)]
    [InlineData(9, 14, 0)]
    [InlineData(9, 15, 30)]
    public void Create_RefusesATimeOffTheStep_ForAnAllDayEvent(int hour, int minute, int second)
    {
        var result = CalendarEventReminder.Create(2, _allDay, "UTC", null, null, 1, null, new TimeOnly(hour, minute, second), _now);

        Assert.True(result.IsError);
        Assert.Equal("Reminder.AllDayTimeOfDay", result.FirstError.Code);
    }

    /// <summary>The form always sends a time for an all-day reminder, so one without it is refused.</summary>
    [Fact]
    public void Create_RefusesAMissingTime_ForAnAllDayEvent()
    {
        var result = CalendarEventReminder.Create(2, _allDay, "UTC", null, null, 1, null, null, _now);

        Assert.True(result.IsError);
        Assert.Equal("Reminder.AllDayTimeOfDay", result.FirstError.Code);
    }

    /// <summary>A timed event keeps every unit and needs no time of day.</summary>
    [Theory]
    [InlineData(30L, null, null, null)]
    [InlineData(null, 2L, null, null)]
    [InlineData(null, null, 1L, null)]
    [InlineData(null, null, null, 1L)]
    public void Create_KeepsEveryUnit_ForATimedEvent(long? minutes, long? hours, long? days, long? weeks)
    {
        var result = CalendarEventReminder.Create(1, _timed, "UTC", minutes, hours, days, weeks, null, _now);

        Assert.False(result.IsError);
    }
}
