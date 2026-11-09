using Maroik.Core.Domain.Calendars;

namespace Maroik.Core.Domain.Tests.Calendars;

/// <summary>An all-day event's day is stored as that date's midnight, marked UTC, whatever zone the viewer is in.</summary>
public class CalendarEventAllDayInstantTests
{
    /// <summary>The instant is the date's midnight and carries the UTC kind.</summary>
    [Fact]
    public void AllDayInstant_IsMidnightOfThatDate_InUtc()
    {
        DateTime instant = CalendarEvent.AllDayInstant(2026, 3, 9);

        Assert.Equal(new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc), instant);
        Assert.Equal(DateTimeKind.Utc, instant.Kind);
    }
}
