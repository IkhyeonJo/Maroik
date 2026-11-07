using Maroik.Core.Contract.Extensions;

namespace Maroik.Core.Contract.Tests.Extensions;

/// <summary>Unit tests for <see cref="TimeZoneDisplayExtensions"/>: each answers with the converted value, not its input.</summary>
public class TimeZoneDisplayExtensionsTests
{
    /// <summary>Noon UTC, away from any daylight-saving change in the zones used here.</summary>
    private static readonly DateTime _utcNoon = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>A UTC value moves into the zone.</summary>
    [Fact]
    public void ToTimeZone_MovesUtcIntoTheZone() =>
        Assert.Equal(new DateTime(2026, 6, 1, 21, 0, 0, DateTimeKind.Unspecified), _utcNoon.ToTimeZone("Asia/Seoul"));

    /// <summary>A local value moves back to UTC.</summary>
    [Fact]
    public void ToUtcFromTimeZone_MovesTheLocalValueToUtc() =>
        Assert.Equal(_utcNoon, new DateTime(2026, 6, 1, 21, 0, 0, DateTimeKind.Unspecified).ToUtcFromTimeZone("Asia/Seoul"));

    /// <summary>A viewer who sees local time gets it; another gets ISO-8601 UTC.</summary>
    [Fact]
    public void ToViewerText_FollowsWhatTheViewerSees()
    {
        Assert.Contains("21:00:00", _utcNoon.ToViewerText(true, "Asia/Seoul"), StringComparison.Ordinal);
        Assert.Equal("2026-06-01T12:00:00Z", _utcNoon.ToViewerText(false, "Asia/Seoul"));
    }

    /// <summary>An unknown zone falls back to its own ID.</summary>
    [Fact]
    public void ToTimeZoneDisplayName_FallsBackToTheId() =>
        Assert.Equal("Not/AZone", "Not/AZone".ToTimeZoneDisplayName());
}
