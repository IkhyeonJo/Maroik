namespace Maroik.Core.Domain.Finances;

/// <summary>
/// The plain facts the dashboard states about one month's spending, each computed from the user's own records and each left out
/// (null) when it cannot honestly be said:
/// <list type="bullet">
/// <item><b>Weekend against weekday</b> — the average spending per Saturday/Sunday against the average per weekday, counting every
/// calendar day of the period (a day without spending counts as zero, so "a weekend day costs twice a weekday" means what it says).
/// Not stated before the period's first weekend day, or when nothing was spent on weekdays (no ratio).</item>
/// <item><b>The largest sub-class</b> and its share of the month's total. Not stated for a month without spending.</item>
/// <item><b>The change from the month before.</b> A complete month is compared with the whole month before; the current, unfinished
/// month only with the same days of the month before (November 1–14 against October 1–14), since half a month against a whole
/// one would always look like a drop. Not stated when the month before spent nothing.</item>
/// </list>
/// The period is the month up to the end of today when it is the current month (a record dated later is not spent yet), the
/// whole month otherwise; a month that has not begun has no facts. Percentages are whole numbers rounded half away from zero,
/// averages are rounded to the stored <see cref="FinanceAmountPolicy.MaxDecimalPlaces"/>. These are descriptive statistics, not
/// significance tests: a single month of one household is too small a sample for a p-value to mean anything.
/// </summary>
public static class ExpenditureInsightPolicy
{
    /// <summary>
    /// The facts for <paramref name="year"/>-<paramref name="month"/> as of <paramref name="today"/>, both in the account's time
    /// zone. <paramref name="records"/> must cover that month and the month before; others are ignored.
    /// </summary>
    public static ExpenditureInsights Insights(IEnumerable<LocalExpenditure> records, int year, int month, DateOnly today)
    {
        DateOnly monthStart = new(year, month, 1);
        if (monthStart > today)
            return new ExpenditureInsights(null, null, null);

        bool isCurrentMonth = year == today.Year && month == today.Month;
        int lastDay = isCurrentMonth ? today.Day : DateTime.DaysInMonth(year, month);
        List<LocalExpenditure> list = [.. records];
        List<LocalExpenditure> period = InDays(list, monthStart, lastDay);

        return new ExpenditureInsights(
            WeekendWeekday(period, monthStart, lastDay),
            TopSubClass(period),
            MonthOverMonth(list, monthStart, lastDay, isCurrentMonth, period.Sum(r => r.Amount)));
    }

    /// <summary>The records of <paramref name="monthStart"/>'s month dated on day 1 to <paramref name="lastDay"/>.</summary>
    private static List<LocalExpenditure> InDays(List<LocalExpenditure> records, DateOnly monthStart, int lastDay) =>
        [.. records.Where(r => r.LocalTime.Year == monthStart.Year && r.LocalTime.Month == monthStart.Month && r.LocalTime.Day <= lastDay)];

    /// <summary>Whether <paramref name="date"/> is a Saturday or a Sunday.</summary>
    private static bool IsWeekend(DateOnly date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    /// <summary>Weekend against weekday daily average over days 1 to <paramref name="lastDay"/>, or null when it cannot be said.</summary>
    private static WeekendWeekdayComparison? WeekendWeekday(List<LocalExpenditure> period, DateOnly monthStart, int lastDay)
    {
        int weekendDays = Enumerable.Range(0, lastDay).Count(offset => IsWeekend(monthStart.AddDays(offset)));
        int weekdayDays = lastDay - weekendDays;
        decimal weekendTotal = period.Where(r => IsWeekend(DateOnly.FromDateTime(r.LocalTime))).Sum(r => r.Amount);
        decimal weekdayTotal = period.Where(r => !IsWeekend(DateOnly.FromDateTime(r.LocalTime))).Sum(r => r.Amount);
        // weekdayTotal > 0 implies a weekday was counted, so weekdayDays > 0 here.
        if (weekendDays == 0 || weekdayTotal == 0)
            return null;

        decimal weekendAverage = weekendTotal / weekendDays;
        decimal weekdayAverage = weekdayTotal / weekdayDays;
        return new WeekendWeekdayComparison(Round(weekendAverage), Round(weekdayAverage), Percent((weekendAverage / weekdayAverage) - 1));
    }

    /// <summary>The sub-class with the most spending in <paramref name="period"/> (ties: the one recorded first) and its share, or null.</summary>
    private static SubClassShare? TopSubClass(List<LocalExpenditure> period)
    {
        decimal total = period.Sum(r => r.Amount);
        if (total == 0)
            return null;

        var top = period
            .GroupBy(r => r.SubClass, StringComparer.Ordinal)
            .Select(g => (SubClass: g.Key, Amount: g.Sum(r => r.Amount)))
            .OrderByDescending(g => g.Amount)
            .First();
        return new SubClassShare(top.SubClass, top.Amount, Percent(top.Amount / total));
    }

    /// <summary>The change from the month before (the same days for the current month), or null when that month spent nothing.</summary>
    private static MonthOverMonthChange? MonthOverMonth(List<LocalExpenditure> records, DateOnly monthStart, int lastDay, bool isCurrentMonth, decimal currentTotal)
    {
        DateOnly previousStart = monthStart.AddMonths(-1);
        int previousDays = DateTime.DaysInMonth(previousStart.Year, previousStart.Month);
        int previousLastDay = isCurrentMonth ? Math.Min(lastDay, previousDays) : previousDays;
        decimal previousTotal = InDays(records, previousStart, previousLastDay).Sum(r => r.Amount);
        if (previousTotal == 0)
            return null;

        return new MonthOverMonthChange(previousTotal, currentTotal, Percent((currentTotal - previousTotal) / previousTotal), isCurrentMonth);
    }

    /// <summary><paramref name="ratio"/> as whole percent, rounded half away from zero.</summary>
    private static int Percent(decimal ratio) => (int)decimal.Round(ratio * 100, 0, MidpointRounding.AwayFromZero);

    /// <summary><paramref name="amount"/> rounded to the stored decimal places.</summary>
    private static decimal Round(decimal amount) => decimal.Round(amount, FinanceAmountPolicy.MaxDecimalPlaces, MidpointRounding.AwayFromZero);
}
