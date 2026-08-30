using Maroik.Core.Domain.Finance;

namespace Maroik.Core.DomainTests.Finance;

/// <summary>
/// Unit tests for <see cref="FixedSchedulePolicy"/>.
/// Covers notice-window detection, unpunctuality override, invalid date combinations,
/// expiration detection (including the exact-boundary case), and row CSS class selection.
/// </summary>
public class FixedSchedulePolicyTests
{
    // -- IsNoticed --------------------------------------------------------------

    /// <summary>Is noticed returns true, when deposit date within window.</summary>
    [Fact]
    public void IsNoticed_ReturnsTrue_WhenDepositDateWithinWindow()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 23, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.True(result);
    }

    /// <summary>Is noticed returns true, when deposit date is exactly at window boundary.</summary>
    [Fact]
    public void IsNoticed_ReturnsTrue_WhenDepositDateIsExactlyAtWindowBoundary()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 25, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.True(result);
    }

    /// <summary>Is noticed returns false, when deposit date just outside window.</summary>
    [Fact]
    public void IsNoticed_ReturnsFalse_WhenDepositDateJustOutsideWindow()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 26, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.False(result);
    }

    /// <summary>Is noticed returns false, when deposit date already passed.</summary>
    [Fact]
    public void IsNoticed_ReturnsFalse_WhenDepositDateAlreadyPassed()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 19, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.False(result);
    }

    /// <summary>
    /// The maturity date is always computed against the current calendar year (no rollover to
    /// next year): a January occurrence that already passed this year stays un-noticed for the
    /// rest of the year, even though the deposit date "looks" close by day/month alone.
    /// </summary>
    [Fact]
    public void IsNoticed_ReturnsFalse_WhenThisYearsOccurrenceAlreadyPassed_EvenIfDayMonthLooksClose()
    {
        var today = new DateTime(2026, 12, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 1, depositDay: 5, today, noticeWindowDays: 30, unpunctuality: false);

        Assert.False(result);
    }

    /// <summary>Same year-end scenario for a different already-passed month/day combination.</summary>
    [Fact]
    public void IsNoticed_ReturnsFalse_WhenThisYearsOccurrenceAlreadyPassed_March()
    {
        var today = new DateTime(2026, 12, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 3, depositDay: 1, today, noticeWindowDays: 30, unpunctuality: false);

        Assert.False(result);
    }

    /// <summary>Is noticed returns true when unpunctuality true regardless of date.</summary>
    [Fact]
    public void IsNoticed_ReturnsTrue_WhenUnpunctualityTrue_RegardlessOfDate()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 1, depositDay: 1, today, noticeWindowDays: 0, unpunctuality: true);

        Assert.True(result);
    }

    /// <summary>Is noticed returns false for invalid date combination.</summary>
    [Fact]
    public void IsNoticed_ReturnsFalse_ForInvalidDateCombination()
    {
        // Feb 29 in a non-leap year (2026) is invalid.
        var today = new DateTime(2026, 2, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 2, depositDay: 29, today, noticeWindowDays: 10, unpunctuality: false);

        Assert.False(result);
    }

    // -- IsValidDepositDate -------------------------------------------------------

    /// <summary>Is valid deposit date returns true, for the last day of a 31-day month.</summary>
    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(10)]
    [InlineData(12)]
    public void IsValidDepositDate_ReturnsTrue_ForDay31Of31DayMonth(int month)
    {
        Assert.True(FixedSchedulePolicy.IsValidDepositDate(month, 31));
    }

    /// <summary>Is valid deposit date returns false, for day 31 of a 30-day month.</summary>
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(9)]
    [InlineData(11)]
    public void IsValidDepositDate_ReturnsFalse_ForDay31Of30DayMonth(int month)
    {
        Assert.False(FixedSchedulePolicy.IsValidDepositDate(month, 31));
    }

    /// <summary>Is valid deposit date returns true, for day 30 of a 30-day month.</summary>
    [Theory]
    [InlineData(4)]
    [InlineData(6)]
    [InlineData(9)]
    [InlineData(11)]
    public void IsValidDepositDate_ReturnsTrue_ForDay30Of30DayMonth(int month)
    {
        Assert.True(FixedSchedulePolicy.IsValidDepositDate(month, 30));
    }

    /// <summary>Is valid deposit date returns true, for day 29 in February (independent of leap year).</summary>
    [Fact]
    public void IsValidDepositDate_ReturnsTrue_ForFebruary29()
    {
        Assert.True(FixedSchedulePolicy.IsValidDepositDate(2, 29));
    }

    /// <summary>Is valid deposit date returns false, for day 30 in February.</summary>
    [Fact]
    public void IsValidDepositDate_ReturnsFalse_ForFebruary30()
    {
        Assert.False(FixedSchedulePolicy.IsValidDepositDate(2, 30));
    }

    /// <summary>Is valid deposit date returns false, for an out-of-range month.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void IsValidDepositDate_ReturnsFalse_ForMonthOutOfRange(int month)
    {
        Assert.False(FixedSchedulePolicy.IsValidDepositDate(month, 1));
    }

    // -- IsExpired ----------------------------------------------------------------

    /// <summary>Is expired returns true, when maturity date before today.</summary>
    [Fact]
    public void IsExpired_ReturnsTrue_WhenMaturityDateBeforeToday()
    {
        var maturityDate = new DateTime(2026, 7, 1);
        var today = new DateTime(2026, 7, 2);

        Assert.True(FixedSchedulePolicy.IsExpired(maturityDate, today));
    }

    /// <summary>Is expired returns false, when maturity date equals today.</summary>
    [Fact]
    public void IsExpired_ReturnsFalse_WhenMaturityDateEqualsToday()
    {
        var date = new DateTime(2026, 7, 2);

        Assert.False(FixedSchedulePolicy.IsExpired(date, date));
    }

    /// <summary>Is expired returns false, when maturity date after today.</summary>
    [Fact]
    public void IsExpired_ReturnsFalse_WhenMaturityDateAfterToday()
    {
        var maturityDate = new DateTime(2026, 7, 3);
        var today = new DateTime(2026, 7, 2);

        Assert.False(FixedSchedulePolicy.IsExpired(maturityDate, today));
    }

    // -- GetRowCssClass -------------------------------------------------------------

    /// <summary>Get row css class returns danger when expired even if also noticed.</summary>
    [Fact]
    public void GetRowCssClass_ReturnsDanger_WhenExpired_EvenIfAlsoNoticed()
    {
        Assert.Equal("table-danger clsGridRow", FixedSchedulePolicy.GetRowCssClass(expired: true, noticed: true));
    }

    /// <summary>Get row css class returns info, when noticed and not expired.</summary>
    [Fact]
    public void GetRowCssClass_ReturnsInfo_WhenNoticedAndNotExpired()
    {
        Assert.Equal("table-info clsGridRow", FixedSchedulePolicy.GetRowCssClass(expired: false, noticed: true));
    }

    /// <summary>Get row css class returns inactive, when neither expired nor noticed.</summary>
    [Fact]
    public void GetRowCssClass_ReturnsInactive_WhenNeitherExpiredNorNoticed()
    {
        Assert.Equal("table-inactive clsGridRow", FixedSchedulePolicy.GetRowCssClass(expired: false, noticed: false));
    }
}
