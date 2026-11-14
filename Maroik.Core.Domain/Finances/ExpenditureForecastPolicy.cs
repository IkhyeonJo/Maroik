namespace Maroik.Core.Domain.Finances;

/// <summary>
/// Projects the current month's month-end spending for the dashboard: what was spent so far, plus what the recent months
/// typically spent after the same day of the month. It is a plain average of the user's own history, shown as an estimate,
/// never stored and never used for anything else.
/// <para>
/// <list type="bullet">
/// <item><b>Spent so far</b> — this month's records up to the end of today (a record dated later, entered ahead, is not spent yet).</item>
/// <item><b>The rest of the month</b> — for each of the last <see cref="RecentMonths"/> months that has at least one record, what it
/// spent after today's day of the month (day &gt; today's day; a 30-day month has nothing after the 31st); the average of those.
/// A month without any record is no basis — the user may not have been keeping the book then — and without any basis there is no
/// projection at all.</item>
/// </list>
/// Only the current month is projected: an earlier month is complete and a later one has not begun. Amounts are as the caller
/// scoped them (the dashboard's default currency); the average is rounded to the stored <see cref="FinanceAmountPolicy.MaxDecimalPlaces"/>.
/// </para>
/// <para>
/// Why so simple, and not a time-series model (SSA, Prophet): an account book has a handful of months, and most spending repeats
/// by day of month (rent, card bills, salary-day shopping). "What the last months spent after today" captures that, is explainable
/// in one sentence, and is exact to test.
/// </para>
/// </summary>
public static class ExpenditureForecastPolicy
{
    /// <summary>How many months before the current one the projection averages.</summary>
    public const int RecentMonths = 3;

    /// <summary>
    /// How many months before the projected month its caller should hand in: the <see cref="RecentMonths"/> it projects from, and the
    /// earlier months whose own projections measure its typical error. Twelve keep both available all year — in January as in July.
    /// </summary>
    public const int HistoryMonths = 12;

    /// <summary>The fewest earlier projections the typical error is stated on: below it, one lucky month would read as accuracy.</summary>
    public const int MinimumErrorMonths = 3;

    /// <summary>
    /// The projection for <paramref name="year"/>-<paramref name="month"/> as of <paramref name="today"/> (both in the account's
    /// time zone), or null when that month is not the current one or no recent month has a record.
    /// <para>
    /// It comes with its typical error: the same projection is made for every earlier month in <paramref name="records"/> that has a
    /// record and a recent month to project from, as of the same day of the month, and compared with what that month actually
    /// spent; the mean absolute error (rounded to the stored decimal places) is stated from <see cref="MinimumErrorMonths"/> such
    /// months on. On the owner's 59 months this projection's error was about half a naive "same as last month" guess (mean absolute scaled error 0.44–0.54).
    /// </para>
    /// </summary>
    public static ExpenditureForecast? Forecast(IEnumerable<LocalExpenditure> records, int year, int month, DateOnly today)
    {
        if (year != today.Year || month != today.Month)
            return null;

        List<LocalExpenditure> list = [.. records];
        DateTime monthStart = new(year, month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        if (Project(list, monthStart, today.Day) is not { } current)
            return null;

        List<decimal> errors = [];
        foreach (DateTime earlier in list.Select(r => new DateTime(r.LocalTime.Year, r.LocalTime.Month, 1, 0, 0, 0, DateTimeKind.Unspecified))
                     .Where(m => m < monthStart).Distinct())
        {
            if (Project(list, earlier, today.Day) is { } replayed)
                errors.Add(Math.Abs(replayed.Projected - InMonth(list, earlier).Sum(r => r.Amount)));
        }

        decimal? typicalError = errors.Count >= MinimumErrorMonths ? Round(errors.Average()) : null;
        return new ExpenditureForecast(current.SpentSoFar, current.Projected, current.BasisMonths, typicalError, errors.Count);
    }

    /// <summary>
    /// The projection for the month starting <paramref name="monthStart"/> as of its day <paramref name="day"/>: spent up to the end of
    /// that day, plus the average of what each of the <see cref="RecentMonths"/> before it that has a record spent after that day; null
    /// when none of them has a record.
    /// </summary>
    private static (decimal SpentSoFar, decimal Projected, int BasisMonths)? Project(List<LocalExpenditure> records, DateTime monthStart, int day)
    {
        decimal spentSoFar = InMonth(records, monthStart).Where(r => r.LocalTime.Day <= day).Sum(r => r.Amount);

        List<decimal> afterDay = [];
        for (int back = 1; back <= RecentMonths; back++)
        {
            List<LocalExpenditure> inMonth = InMonth(records, monthStart.AddMonths(-back));
            if (inMonth.Count > 0)
                afterDay.Add(inMonth.Where(r => r.LocalTime.Day > day).Sum(r => r.Amount));
        }

        return afterDay.Count == 0 ? null : (spentSoFar, spentSoFar + Round(afterDay.Average()), afterDay.Count);
    }

    /// <summary>The records dated in the month starting <paramref name="monthStart"/>.</summary>
    private static List<LocalExpenditure> InMonth(List<LocalExpenditure> records, DateTime monthStart) =>
        [.. records.Where(r => r.LocalTime >= monthStart && r.LocalTime < monthStart.AddMonths(1))];

    /// <summary><paramref name="amount"/> rounded to the stored decimal places, half away from zero.</summary>
    private static decimal Round(decimal amount) => decimal.Round(amount, FinanceAmountPolicy.MaxDecimalPlaces, MidpointRounding.AwayFromZero);
}
