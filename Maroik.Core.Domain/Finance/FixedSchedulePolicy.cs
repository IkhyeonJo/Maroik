namespace Maroik.Core.Domain.Finance;

/// <summary>
/// Domain rules for determining the notification status of a recurring fixed-income or
/// fixed-expenditure schedule.
/// These rules are shared between the presentation layer (notice grid display)
/// and the application layer (dashboard badge counts).
/// </summary>
public static class FixedSchedulePolicy
{
    /// <summary>
    /// Returns <see langword="true"/> when the next scheduled deposit for
    /// (<paramref name="depositMonth"/>, <paramref name="depositDay"/>) falls within
    /// <paramref name="noticeWindowDays"/> days of <paramref name="today"/>,
    /// or when <paramref name="unpunctuality"/> is set.
    /// Returns <see langword="false"/> for invalid date combinations (e.g. Feb 29 in a non-leap year).
    /// </summary>
    public static bool IsNoticed(
        int depositMonth,
        int depositDay,
        DateTime today,
        int noticeWindowDays,
        bool unpunctuality)
    {
        if (unpunctuality) return true;
        try
        {
            var depositDate = new DateTime(today.Year, depositMonth, depositDay);
            double daysUntil = (depositDate - today).TotalDays;
            return daysUntil >= 0 && daysUntil <= noticeWindowDays;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="maturityDate"/> is strictly
    /// before <paramref name="today"/> (i.e. the schedule has expired).
    /// </summary>
    public static bool IsExpired(DateTime maturityDate, DateTime today)
        => (maturityDate - today).TotalDays < 0;

    /// <summary>
    /// Returns the notice-grid row CSS class for a schedule, prioritizing
    /// <paramref name="expired"/> over <paramref name="noticed"/> over the default state.
    /// </summary>
    public static string GetRowCssClass(bool expired, bool noticed) =>
        expired ? "table-danger clsGridRow" : noticed ? "table-info clsGridRow" : "table-inactive clsGridRow";
}
