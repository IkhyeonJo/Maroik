using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>Unit tests for <see cref="TimeZoneDisplayService"/>: each member answers what the Domain answers.</summary>
public class TimeZoneDisplayServiceTests
{
    /// <summary>The service under test.</summary>
    private readonly TimeZoneDisplayService _sut = new();

    /// <summary>An all-day date is the domain's instant: that date's midnight, marked UTC.</summary>
    [Fact]
    public void ToAllDayInstant_IsTheDatesMidnight_InUtc()
    {
        DateTime instant = _sut.ToAllDayInstant(2026, 3, 9);

        Assert.Equal(new DateTime(2026, 3, 9, 0, 0, 0, DateTimeKind.Utc), instant);
        Assert.Equal(DateTimeKind.Utc, instant.Kind);
    }

    /// <summary>Noon UTC, away from any daylight-saving change in the zones used here.</summary>
    private static readonly DateTime _utcNoon = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A UTC value moves into the zone.</summary>
    [Fact]
    public void ToTimeZone_MovesUtcIntoTheZone() =>
        Assert.Equal(new DateTime(2026, 6, 1, 21, 0, 0, DateTimeKind.Unspecified), _sut.ToTimeZone(_utcNoon, "Asia/Seoul"));

    /// <summary>A local value moves back to UTC.</summary>
    [Fact]
    public void ToUtcFromTimeZone_MovesTheLocalValueToUtc() =>
        Assert.Equal(_utcNoon, _sut.ToUtcFromTimeZone(new DateTime(2026, 6, 1, 21, 0, 0, DateTimeKind.Unspecified), "Asia/Seoul"));

    /// <summary>A viewer who sees local time gets it; another gets ISO-8601 UTC.</summary>
    [Fact]
    public void FormatForViewer_FollowsWhatTheViewerSees()
    {
        Assert.Contains("21:00:00", _sut.FormatForViewer(_utcNoon, true, "Asia/Seoul"), StringComparison.Ordinal);
        Assert.Equal("2026-06-01T12:00:00Z", _sut.FormatForViewer(_utcNoon, false, "Asia/Seoul"));
    }

    /// <summary>An unknown zone falls back to its own ID.</summary>
    [Fact]
    public void GetTimeZoneDisplayName_FallsBackToTheId() =>
        Assert.Equal("Not/AZone", _sut.GetTimeZoneDisplayName("Not/AZone"));
}
