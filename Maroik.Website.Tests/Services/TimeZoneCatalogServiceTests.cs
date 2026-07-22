using Maroik.Website.Services;

namespace Maroik.Website.Tests.Services;

/// <summary>
/// Unit tests for <see cref="TimeZoneCatalogService"/> — the server-side replacement
/// for the Windows-to-IANA time zone enumeration previously duplicated across ~11 Razor views.
/// </summary>
public class TimeZoneCatalogServiceTests
{
    private static TimeZoneCatalogService CreateSut() => new();

    [Fact]
    public void GetTimeZoneOptions_ReturnsNonEmptyList()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.NotEmpty(options);
    }

    [Fact]
    public void GetTimeZoneOptions_NoEntryHasEmptyIanaId()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.All(options, option => Assert.False(string.IsNullOrEmpty(option.IanaId)));
    }

    [Fact]
    public void GetTimeZoneOptions_NoEntryHasEmptyDisplayName()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.All(options, option => Assert.False(string.IsNullOrEmpty(option.DisplayName)));
    }

    [Fact]
    public void GetTimeZoneOptions_ContainsKnownIanaZone()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.Contains(options, option => option.IanaId == "Asia/Seoul");
    }

    [Fact]
    public void GetTimeZoneOptions_ContainsUtc()
    {
        var options = CreateSut().GetTimeZoneOptions();

        Assert.Contains(options, option => option.IanaId == "UTC");
    }

    [Fact]
    public void GetTimeZoneOptions_ReturnsSameCachedInstance_OnRepeatedCalls()
    {
        var sut = CreateSut();

        var first = sut.GetTimeZoneOptions();
        var second = sut.GetTimeZoneOptions();

        Assert.Same(first, second);
    }
}
