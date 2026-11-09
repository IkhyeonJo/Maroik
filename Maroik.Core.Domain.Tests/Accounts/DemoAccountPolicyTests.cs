using Maroik.Core.Domain.Accounts;

namespace Maroik.Core.Domain.Tests.Accounts;

/// <summary>The demo account's pinned period, as read from its configuration.</summary>
public class DemoAccountPolicyTests
{
    /// <summary>Only the configured address is the demo account; with none configured, nobody is.</summary>
    [Theory]
    [InlineData("demo@maroik.com", "demo@maroik.com", true)]
    [InlineData("demo@maroik.com", "user@maroik.com", false)]
    [InlineData("", "", false)]
    [InlineData(null, null, false)]
    public void IsDemoAccount_IsTheConfiguredAddress(string? demo, string? account, bool expected)
    {
        Assert.Equal(expected, DemoAccountPolicy.IsDemoAccount(demo, account));
    }

    /// <summary>A real month gives its first day; anything else pins nothing.</summary>
    [Theory]
    [InlineData("2025", "3", "2025-03-01")]
    [InlineData("2025", "13", null)]
    [InlineData("abcd", "1", null)]
    [InlineData(null, null, null)]
    public void FirstDayOf_IsTheFirstOfARealMonth(string? year, string? month, string? expected)
    {
        Assert.Equal(expected is null ? null : DateOnly.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), DemoAccountPolicy.FirstDayOf(year, month));
    }

    /// <summary>A year from 1 to 9999 is pinned; anything else is not.</summary>
    [Theory]
    [InlineData("2025", 2025)]
    [InlineData("1", 1)]
    [InlineData("9999", 9999)]
    [InlineData("0", null)]
    [InlineData("10000", null)]
    [InlineData("-1", null)]
    [InlineData(null, null)]
    public void YearOf_IsARealYear(string? year, int? expected)
    {
        Assert.Equal(expected, DemoAccountPolicy.YearOf(year));
    }
}
