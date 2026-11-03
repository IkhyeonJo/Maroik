using Maroik.Core.Domain.Calendars;
namespace Maroik.Core.Domain.Tests.Calendars;

/// <summary>
/// Unit tests for the calendar access and provisioning rules: <see cref="CalendarAccessPolicy"/>,
/// <see cref="Domain.Calendars.Calendar.IsOwnedBy"/>, <see cref="Domain.Calendars.Calendar.IsNameTaken"/>,
/// <see cref="Domain.Calendars.Calendar.NeedsDefault"/> and <see cref="Domain.Calendars.Calendar.CreateDefault"/>.
/// </summary>
public class CalendarAccessTests
{
    /// <summary>The fixed "current time" every domain call in this class receives.</summary>
    private static readonly DateTime _now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A stored calendar of "user@example.com".</summary>
    private static Calendar CalendarOf(long id, string name = "Work") =>
        Calendar.Reconstitute(id, "user@example.com", name, null, "UTC", "#112233", _now, _now);

    /// <summary>A stored event in calendar <paramref name="calendarId"/>.</summary>
    private static CalendarEvent EventIn(long calendarId) =>
        CalendarEvent.Reconstitute(7, calendarId, "Meeting", null, false, _now, _now.AddHours(1), "UTC", "UTC", null, null, null, _now, _now);

    /// <summary>The account's calendars 1 and 2.</summary>
    private static readonly Calendar[] _owned = [CalendarOf(1), CalendarOf(2, "Home")];

    // -- Ownership --------------------------------------------------------------

    /// <summary>A calendar is owned when it is among the account's calendars.</summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    public void Owns_IsTrue_OnlyForTheAccountsCalendars(long calendarId, bool owned)
        => Assert.Equal(owned, CalendarAccessPolicy.Owns(_owned, calendarId));

    /// <summary>Every target must be owned; one foreign calendar is enough to refuse.</summary>
    [Fact]
    public void OwnsAll_RefusesWhenAnyCalendarIsForeign()
    {
        Assert.True(CalendarAccessPolicy.OwnsAll(_owned, [1, 2]));
        Assert.True(CalendarAccessPolicy.OwnsAll(_owned, []));
        Assert.False(CalendarAccessPolicy.OwnsAll(_owned, [1, 3]));
    }

    /// <summary>An event is owned when it exists and sits in one of the account's calendars.</summary>
    [Fact]
    public void OwnsEvent_RequiresAnExistingEventInAnOwnedCalendar()
    {
        Assert.True(CalendarAccessPolicy.OwnsEvent(_owned, EventIn(2)));
        Assert.False(CalendarAccessPolicy.OwnsEvent(_owned, EventIn(3)));
        Assert.False(CalendarAccessPolicy.OwnsEvent(_owned, null));
    }

    /// <summary>A calendar is owned by the account whose email it carries.</summary>
    [Fact]
    public void IsOwnedBy_MatchesTheAccountEmail()
    {
        Assert.True(CalendarOf(1).IsOwnedBy("user@example.com"));
        Assert.False(CalendarOf(1).IsOwnedBy("other@example.com"));
    }

    // -- Names ------------------------------------------------------------------

    /// <summary>A name is taken when one of the account's calendars already has it, compared exactly.</summary>
    [Theory]
    [InlineData("Work", true)]
    [InlineData("work", false)]
    [InlineData("Travel", false)]
    [InlineData(null, false)]
    public void IsNameTaken_ComparesTheNamesExactly(string? name, bool taken)
        => Assert.Equal(taken, Calendar.IsNameTaken(_owned, name));

    // -- Default calendar -------------------------------------------------------

    /// <summary>The default calendar is only needed by an account that has none.</summary>
    [Fact]
    public void NeedsDefault_OnlyWhenTheAccountHasNoCalendar()
    {
        Assert.True(Calendar.NeedsDefault([]));
        Assert.False(Calendar.NeedsDefault(_owned));
    }

    /// <summary>An unusable time zone falls back to UTC; a usable one is kept.</summary>
    [Theory]
    [InlineData(null, "UTC")]
    [InlineData("Not/AZone", "UTC")]
    [InlineData("Asia/Seoul", "Asia/Seoul")]
    public void CreateDefault_FallsBackToUtc_ForAnUnusableTimeZone(string? timeZone, string expected)
    {
        var result = Calendar.CreateDefault("user@example.com", "My calendar", null, timeZone, "#112233", _now);

        Assert.Equal(expected, result.Value.TimeZone.Value);
    }

    /// <summary>Every other field is still validated as for a normal calendar.</summary>
    [Fact]
    public void CreateDefault_StillRejectsAnInvalidName()
        => Assert.True(Calendar.CreateDefault("user@example.com", " ", null, "UTC", "#112233", _now).IsError);
}
