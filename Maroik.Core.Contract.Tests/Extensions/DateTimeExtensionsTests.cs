using System.Globalization;
using Maroik.Core.Domain.Account;
using Maroik.Core.Contract.Misc.Extensions;

namespace Maroik.Core.Contract.Tests.Extensions;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Misc.Extensions.DateTimeExtensions"/>.
/// Verifies UTC-to-local conversion using IANA time-zone IDs and graceful handling
/// of unknown or null zone identifiers.
/// </summary>
public class DateTimeExtensionsTests
{
    // -- ConvertTimeByTimeZoneIanaId ------------------------------------------

    /// <summary>Verifies that <c>ConvertTimeByTimeZoneIanaId</c> converts utc to local time for valid iana id.</summary>
    [Fact]
    public void ConvertTimeByTimeZoneIanaId_ConvertsUtcToLocalTime_ForValidIanaId()
    {
        // UTC noon should be 9 PM in Asia/Seoul (UTC+9)
        var utcNoon = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        DateTime result = utcNoon.ConvertTimeByTimeZoneIanaId("Asia/Seoul");

        // Asia/Seoul is UTC+9; noon UTC = 21:00 KST
        Assert.Equal(21, result.Hour);
        Assert.Equal(0, result.Minute);
    }

    /// <summary>Verifies that <c>ConvertTimeByTimeZoneIanaId</c> returns original value for invalid iana id.</summary>
    [Fact]
    public void ConvertTimeByTimeZoneIanaId_ReturnsOriginalValue_ForInvalidIanaId()
    {
        var original = new DateTime(2025, 1, 1, 10, 0, 0);

        DateTime result = original.ConvertTimeByTimeZoneIanaId("Invalid/Timezone/ID");

        Assert.Equal(original, result);
    }

    /// <summary>Verifies that <c>ConvertTimeByTimeZoneIanaId</c> returns original value for empty iana id.</summary>
    [Fact]
    public void ConvertTimeByTimeZoneIanaId_ReturnsOriginalValue_ForEmptyIanaId()
    {
        var original = new DateTime(2025, 3, 15, 8, 30, 0);

        DateTime result = original.ConvertTimeByTimeZoneIanaId("");

        Assert.Equal(original, result);
    }

    /// <summary>Verifies that <c>ConvertTimeByTimeZoneIanaId</c> returns utc time for utc iana id.</summary>
    [Fact]
    public void ConvertTimeByTimeZoneIanaId_ReturnsUtcTime_ForUtcIanaId()
    {
        var utcTime = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        DateTime result = utcTime.ConvertTimeByTimeZoneIanaId("UTC");

        Assert.Equal(12, result.Hour);
        Assert.Equal(0, result.Minute);
    }

    /// <summary>Verifies that <c>ConvertTimeByTimeZoneIanaId</c> handles negative offsets.</summary>
    [Fact]
    public void ConvertTimeByTimeZoneIanaId_HandlesNegativeOffsets()
    {
        // UTC noon in America/New_York (UTC-5 in winter) should be 7 AM
        var utcNoon = new DateTime(2025, 1, 15, 12, 0, 0, DateTimeKind.Utc);

        DateTime result = utcNoon.ConvertTimeByTimeZoneIanaId("America/New_York");

        Assert.Equal(7, result.Hour);
    }

    // -- ToViewerFormattedString ------------------------------------------------

    /// <summary>Verifies that Admin viewers see the value converted to their local time zone.</summary>
    [Fact]
    public void ToViewerFormattedString_AdminViewer_ReturnsLocalTimeConvertedValue()
    {
        var utcNoon = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        string result = utcNoon.ToViewerFormattedString(Role.Admin, "Asia/Seoul");

        Assert.Equal(utcNoon.ConvertTimeByTimeZoneIanaId("Asia/Seoul").ToString(CultureInfo.InvariantCulture), result);
    }

    /// <summary>Verifies that User viewers see the value converted to their local time zone, same as Admin.</summary>
    [Fact]
    public void ToViewerFormattedString_UserViewer_ReturnsLocalTimeConvertedValue()
    {
        var utcNoon = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        string result = utcNoon.ToViewerFormattedString(Role.User, "Asia/Seoul");

        Assert.Equal(utcNoon.ConvertTimeByTimeZoneIanaId("Asia/Seoul").ToString(CultureInfo.InvariantCulture), result);
    }

    /// <summary>Verifies that Anonymous viewers see the raw, unconverted UTC value in ISO-8601 form.</summary>
    [Fact]
    public void ToViewerFormattedString_AnonymousViewer_ReturnsRawUtcIso8601()
    {
        var utcNoon = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        string result = utcNoon.ToViewerFormattedString(Role.Anonymous, "Asia/Seoul");

        Assert.Equal("2025-06-15T12:00:00Z", result);
    }

    /// <summary>Verifies that a null role is treated the same as Anonymous (raw UTC, not converted).</summary>
    [Fact]
    public void ToViewerFormattedString_NullRole_ReturnsRawUtcIso8601()
    {
        var utcNoon = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        string result = utcNoon.ToViewerFormattedString(null, "Asia/Seoul");

        Assert.Equal("2025-06-15T12:00:00Z", result);
    }

    // -- ConvertToUtcByTimeZoneIanaId --------------------------------------------

    /// <summary>Verifies that <c>ConvertToUtcByTimeZoneIanaId</c> converts local time to UTC for a valid IANA id.</summary>
    [Fact]
    public void ConvertToUtcByTimeZoneIanaId_ConvertsLocalToUtc_ForValidIanaId()
    {
        // 21:00 in Asia/Seoul (UTC+9) should be noon UTC
        var localSeoul = new DateTime(2025, 6, 15, 21, 0, 0, DateTimeKind.Unspecified);

        DateTime result = localSeoul.ConvertToUtcByTimeZoneIanaId("Asia/Seoul");

        Assert.Equal(12, result.Hour);
        Assert.Equal(0, result.Minute);
    }

    /// <summary>Verifies that <c>ConvertToUtcByTimeZoneIanaId</c> is the inverse of <c>ConvertTimeByTimeZoneIanaId</c>.</summary>
    [Fact]
    public void ConvertToUtcByTimeZoneIanaId_RoundTrips_WithConvertTimeByTimeZoneIanaId()
    {
        var utcNoon = new DateTime(2025, 6, 15, 12, 0, 0, DateTimeKind.Utc);

        DateTime local = utcNoon.ConvertTimeByTimeZoneIanaId("America/New_York");
        DateTime roundTripped = local.ConvertToUtcByTimeZoneIanaId("America/New_York");

        Assert.Equal(utcNoon.Hour, roundTripped.Hour);
        Assert.Equal(utcNoon.Minute, roundTripped.Minute);
    }

    /// <summary>Verifies that <c>ConvertToUtcByTimeZoneIanaId</c> returns original value for invalid iana id.</summary>
    [Fact]
    public void ConvertToUtcByTimeZoneIanaId_ReturnsOriginalValue_ForInvalidIanaId()
    {
        var original = new DateTime(2025, 1, 1, 10, 0, 0);

        DateTime result = original.ConvertToUtcByTimeZoneIanaId("Invalid/Timezone/ID");

        Assert.Equal(original, result);
    }

    // -- ToTimeZoneStandardName ---------------------------------------------------

    /// <summary>Verifies that <c>ToTimeZoneStandardName</c> returns the standard display name for a valid IANA id.</summary>
    [Fact]
    public void ToTimeZoneStandardName_ReturnsStandardName_ForValidIanaId()
    {
        string result = "Asia/Seoul".ToTimeZoneStandardName();

        Assert.Equal(TimeZoneInfo.FindSystemTimeZoneById("Asia/Seoul").StandardName, result);
    }

    /// <summary>Verifies that <c>ToTimeZoneStandardName</c> falls back to the raw id for an invalid IANA id.</summary>
    [Fact]
    public void ToTimeZoneStandardName_ReturnsRawId_ForInvalidIanaId()
    {
        string result = "Invalid/Timezone/ID".ToTimeZoneStandardName();

        Assert.Equal("Invalid/Timezone/ID", result);
    }
}
