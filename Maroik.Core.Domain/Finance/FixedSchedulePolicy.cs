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
    /// Returns <see langword="true"/> when this year's scheduled deposit for
    /// (<paramref name="depositMonth"/>, <paramref name="depositDay"/>) has not yet passed and
    /// falls within <paramref name="noticeWindowDays"/> days of <paramref name="today"/>,
    /// or when <paramref name="unpunctuality"/> is set.
    /// Once this year's occurrence has passed, it stays un-noticed until next year (matches main).
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
    /// The highest valid deposit day for <paramref name="depositMonth"/> (1–12): 31 for 31-day
    /// months, 30 for 30-day months, 29 for February (regardless of leap year — matches main's
    /// per-month day-range validation), and 0 for an out-of-range month.
    /// <para>
    /// Single source of truth for the month→day rule: <see cref="IsValidDepositDate"/> is derived
    /// from it, and the presentation layer serializes <see cref="MaxDepositDayByMonth"/> into the
    /// create/edit forms so the client no longer hard-codes the same month groups.
    /// </para>
    /// </summary>
    public static int MaxDepositDay(int depositMonth) => depositMonth switch
    {
        1 or 3 or 5 or 7 or 8 or 10 or 12 => 31,
        4 or 6 or 9 or 11 => 30,
        2 => 29,
        _ => 0
    };

    /// <summary>
    /// <see cref="MaxDepositDay(int)"/> for every month 1–12, for serialization into the UI.
    /// </summary>
    public static readonly IReadOnlyDictionary<int, int> MaxDepositDayByMonth =
        Enumerable.Range(1, 12).ToDictionary(month => month, MaxDepositDay);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="depositDay"/> is a valid day for
    /// <paramref name="depositMonth"/> (30/31-day months, and February capped at 29 regardless
    /// of leap year — matches main's per-month day-range validation).
    /// </summary>
    public static bool IsValidDepositDate(int depositMonth, int depositDay)
        => depositDay >= 1 && depositDay <= MaxDepositDay(depositMonth);

    /// <summary>
    /// Returns the notice-grid row CSS class for a schedule, prioritizing
    /// <paramref name="expired"/> over <paramref name="noticed"/> over the default state.
    /// </summary>
    public static string GetRowCssClass(bool expired, bool noticed) =>
        expired ? "table-danger clsGridRow" : noticed ? "table-info clsGridRow" : "table-inactive clsGridRow";
}
