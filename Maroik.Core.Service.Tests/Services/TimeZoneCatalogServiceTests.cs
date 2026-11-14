using System.Collections.ObjectModel;
using Maroik.Core.Contract.Dtos;
using Maroik.Core.Service.Services;

namespace Maroik.Core.Service.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TimeZoneCatalogService"/> — the server-side
/// time zone list (IANA IDs) the Razor views render their time-zone pickers from.
/// </summary>
public class TimeZoneCatalogServiceTests
{
    /// <summary>The service under test.</summary>
    private static TimeZoneCatalogService CreateSut() => new();

    /// <summary>Get time zone options returns non-empty list.</summary>
    [Fact]
    public void GetTimeZoneOptions_ReturnsNonEmptyList()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.NotEmpty(options);
    }

    /// <summary>
    /// Each option carries an IANA ID on every OS, once. A system zone whose own ID is already an IANA ID (all of them on Linux)
    /// is listed under it, in the system's order; there are never more options than system zones.
    /// </summary>
    [Fact]
    public void GetTimeZoneOptions_ListsTheSystemZones_UnderTheirIanaIds()
    {
        IReadOnlyList<TimeZoneOptionDto> options = CreateSut().GetTimeZoneOptions();
        ReadOnlyCollection<TimeZoneInfo> systemZones = TimeZoneInfo.GetSystemTimeZones();
        List<string> ianaSystemIds = [.. systemZones.Where(z => z.HasIanaId).Select(z => z.Id)];

        Assert.Equal(ianaSystemIds, options.Select(o => o.IanaId).Where(ianaSystemIds.Contains));
        Assert.InRange(options.Count, 1, systemZones.Count);
        Assert.Equal(options.Count, options.Select(o => o.IanaId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(options, o => Assert.True(TimeZoneInfo.FindSystemTimeZoneById(o.IanaId).HasIanaId, o.IanaId));
    }

    /// <summary>Get time zone options no entry has empty iana id.</summary>
    [Fact]
    public void GetTimeZoneOptions_NoEntryHasEmptyIanaId()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.All(options, option => Assert.False(string.IsNullOrEmpty(option.IanaId)));
    }

    /// <summary>Get time zone options no entry has empty display name.</summary>
    [Fact]
    public void GetTimeZoneOptions_NoEntryHasEmptyDisplayName()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.All(options, option => Assert.False(string.IsNullOrEmpty(option.DisplayName)));
    }

    /// <summary>Get time zone options contains known iana zone.</summary>
    [Fact]
    public void GetTimeZoneOptions_ContainsKnownIanaZone()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.Contains(options, option => option.IanaId == "Asia/Seoul");
    }

    /// <summary>Get time zone options contains utc.</summary>
    [Fact]
    public void GetTimeZoneOptions_ContainsUtc()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.Contains(options, option => option.IanaId == "UTC");
    }

    /// <summary>Get time zone options returns same cached instance on repeated calls.</summary>
    [Fact]
    public void GetTimeZoneOptions_ReturnsSameCachedInstance_OnRepeatedCalls()
    {
        var sut = CreateSut();

        var first = sut.GetTimeZoneOptions();
        var second = sut.GetTimeZoneOptions();

        Assert.Same(first, second);
    }
}
