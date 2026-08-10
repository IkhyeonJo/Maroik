using Maroik.Core.Contract.Misc.Policies;

namespace Maroik.Core.Contract.Tests.Policies;

/// <summary>
/// Unit tests for <see cref="FixedSchedulePolicy"/>.
/// Covers notice-window detection, unpunctuality override, invalid date combinations,
/// expiration detection (including the exact-boundary case), and row CSS class selection.
/// </summary>
public class FixedSchedulePolicyTests
{
    // -- IsNoticed --------------------------------------------------------------

    [Fact]
    public void IsNoticed_ReturnsTrue_WhenDepositDateWithinWindow()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 23, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.True(result);
    }

    [Fact]
    public void IsNoticed_ReturnsTrue_WhenDepositDateIsExactlyAtWindowBoundary()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 25, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.True(result);
    }

    [Fact]
    public void IsNoticed_ReturnsFalse_WhenDepositDateJustOutsideWindow()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 26, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.False(result);
    }

    [Fact]
    public void IsNoticed_ReturnsFalse_WhenDepositDateAlreadyPassed()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 7, depositDay: 19, today, noticeWindowDays: 5, unpunctuality: false);

        Assert.False(result);
    }

    [Fact]
    public void IsNoticed_ReturnsTrue_WhenUnpunctualityTrue_RegardlessOfDate()
    {
        var today = new DateTime(2026, 7, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 1, depositDay: 1, today, noticeWindowDays: 0, unpunctuality: true);

        Assert.True(result);
    }

    [Fact]
    public void IsNoticed_ReturnsFalse_ForInvalidDateCombination()
    {
        // Feb 29 in a non-leap year (2026) is invalid.
        var today = new DateTime(2026, 2, 20);

        var result = FixedSchedulePolicy.IsNoticed(depositMonth: 2, depositDay: 29, today, noticeWindowDays: 10, unpunctuality: false);

        Assert.False(result);
    }

    // -- IsExpired ----------------------------------------------------------------

    [Fact]
    public void IsExpired_ReturnsTrue_WhenMaturityDateBeforeToday()
    {
        var maturityDate = new DateTime(2026, 7, 1);
        var today = new DateTime(2026, 7, 2);

        Assert.True(FixedSchedulePolicy.IsExpired(maturityDate, today));
    }

    [Fact]
    public void IsExpired_ReturnsFalse_WhenMaturityDateEqualsToday()
    {
        var date = new DateTime(2026, 7, 2);

        Assert.False(FixedSchedulePolicy.IsExpired(date, date));
    }

    [Fact]
    public void IsExpired_ReturnsFalse_WhenMaturityDateAfterToday()
    {
        var maturityDate = new DateTime(2026, 7, 3);
        var today = new DateTime(2026, 7, 2);

        Assert.False(FixedSchedulePolicy.IsExpired(maturityDate, today));
    }

    // -- GetRowCssClass -------------------------------------------------------------

    [Fact]
    public void GetRowCssClass_ReturnsDanger_WhenExpired_EvenIfAlsoNoticed()
    {
        Assert.Equal("table-danger clsGridRow", FixedSchedulePolicy.GetRowCssClass(expired: true, noticed: true));
    }

    [Fact]
    public void GetRowCssClass_ReturnsInfo_WhenNoticedAndNotExpired()
    {
        Assert.Equal("table-info clsGridRow", FixedSchedulePolicy.GetRowCssClass(expired: false, noticed: true));
    }

    [Fact]
    public void GetRowCssClass_ReturnsInactive_WhenNeitherExpiredNorNoticed()
    {
        Assert.Equal("table-inactive clsGridRow", FixedSchedulePolicy.GetRowCssClass(expired: false, noticed: false));
    }
}
