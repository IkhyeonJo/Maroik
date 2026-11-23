using Maroik.Core.Domain.Assistants;

namespace Maroik.Core.Domain.Tests.Assistants;

/// <summary>
/// The server, not the model, turns "this month", "last week" or two dates into the exact days a tool reads: a small model gets
/// relative dates wrong, so it only names the period.
/// </summary>
public class AssistantPeriodTests
{
    /// <summary>Wednesday 2026-11-25 in the account's time zone.</summary>
    private static readonly DateOnly _today = new(2026, 11, 25);

    /// <summary>Each named period resolves to its first and last day, counted from the account's today.</summary>
    [Theory]
    [InlineData("today", "2026-11-25", "2026-11-25")]
    [InlineData("yesterday", "2026-11-24", "2026-11-24")]
    [InlineData("this_week", "2026-11-23", "2026-11-29")]
    [InlineData("last_week", "2026-11-16", "2026-11-22")]
    [InlineData("this_month", "2026-11-01", "2026-11-30")]
    [InlineData("last_month", "2026-10-01", "2026-10-31")]
    [InlineData("this_year", "2026-01-01", "2026-12-31")]
    [InlineData("last_year", "2025-01-01", "2025-12-31")]
    [InlineData("last_7_days", "2026-11-19", "2026-11-25")]
    [InlineData("last_30_days", "2026-10-27", "2026-11-25")]
    public void ANamedPeriod_ResolvesFromToday(string period, string start, string end)
    {
        AssistantPeriod resolved = AssistantPeriod.Resolve(period, null, null, _today).Value;

        Assert.Equal(DateOnly.Parse(start, System.Globalization.CultureInfo.InvariantCulture), resolved.StartDate);
        Assert.Equal(DateOnly.Parse(end, System.Globalization.CultureInfo.InvariantCulture), resolved.EndDate);
    }

    /// <summary>A week starts on Monday: on a Sunday "this week" is the six days before and that Sunday.</summary>
    [Fact]
    public void OnASunday_ThisWeek_StartsOnTheMondayBefore()
    {
        AssistantPeriod resolved = AssistantPeriod.Resolve("this_week", null, null, new DateOnly(2026, 11, 29)).Value;

        Assert.Equal(new DateOnly(2026, 11, 23), resolved.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 29), resolved.EndDate);
    }

    /// <summary>The name is matched without regard to case or surrounding spaces, the way a model may write it.</summary>
    [Fact]
    public void TheName_IsMatchedLoosely()
    {
        Assert.Equal(new DateOnly(2026, 11, 1), AssistantPeriod.Resolve(" This_Month ", null, null, _today).Value.StartDate);
    }

    /// <summary>No period and no dates means this month.</summary>
    [Fact]
    public void Nothing_MeansThisMonth()
    {
        AssistantPeriod resolved = AssistantPeriod.Resolve(null, null, " ", _today).Value;

        Assert.Equal(new DateOnly(2026, 11, 1), resolved.StartDate);
        Assert.Equal(new DateOnly(2026, 11, 30), resolved.EndDate);
    }

    /// <summary>Two dates win over a period name and are taken as they are, both days included.</summary>
    [Fact]
    public void TwoDates_WinOverAPeriodName()
    {
        AssistantPeriod resolved = AssistantPeriod.Resolve("this_year", "2026-03-02", "2026-03-15", _today).Value;

        Assert.Equal(new DateOnly(2026, 3, 2), resolved.StartDate);
        Assert.Equal(new DateOnly(2026, 3, 15), resolved.EndDate);
    }

    /// <summary>An unknown name is refused with the names that are allowed, so the model can try again.</summary>
    [Fact]
    public void AnUnknownName_IsRefused_WithTheAllowedNames()
    {
        var result = AssistantPeriod.Resolve("next_decade", null, null, _today);

        Assert.True(result.IsError);
        Assert.Equal("Assistant.UnknownPeriod", result.FirstError.Code);
        Assert.Contains("last_30_days", result.FirstError.Description, StringComparison.Ordinal);
    }

    /// <summary>Only one of the two dates, or a date that is not yyyy-MM-dd, is refused.</summary>
    [Theory]
    [InlineData("2026-03-02", null)]
    [InlineData(null, "2026-03-02")]
    [InlineData("2026/03/02", "2026-03-05")]
    [InlineData("2026-03-02", "March 5")]
    public void AnIncompleteOrMalformedDatePair_IsRefused(string? start, string? end)
    {
        var result = AssistantPeriod.Resolve(null, start, end, _today);

        Assert.Equal("Assistant.InvalidDates", result.FirstError.Code);
    }

    /// <summary>A start after the end is refused.</summary>
    [Fact]
    public void AStartAfterTheEnd_IsRefused()
    {
        Assert.Equal("Assistant.StartAfterEnd", AssistantPeriod.Resolve(null, "2026-03-05", "2026-03-04", _today).FirstError.Code);
    }

    /// <summary>A range may span at most <see cref="AssistantPolicy.MaxPeriodDays"/> days, both ends included.</summary>
    [Fact]
    public void ARangeLongerThanTheLimit_IsRefused()
    {
        Assert.False(AssistantPeriod.Resolve(null, "2025-01-01", "2026-01-01", _today).IsError);
        Assert.Equal("Assistant.PeriodTooLong", AssistantPeriod.Resolve(null, "2025-01-01", "2026-01-02", _today).FirstError.Code);
    }

    /// <summary>The period as UTC bounds: from local midnight of the first day up to (not including) local midnight after the last.</summary>
    [Fact]
    public void ToUtcRange_CoversTheLocalDays()
    {
        AssistantPeriod period = AssistantPeriod.Resolve("today", null, null, _today).Value;

        (DateTime start, DateTime end) = period.ToUtcRange("Asia/Seoul");

        Assert.Equal(new DateTime(2026, 11, 24, 15, 0, 0), start);
        Assert.Equal(new DateTime(2026, 11, 25, 15, 0, 0), end);
    }
}
