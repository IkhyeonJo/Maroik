using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>
/// Unit tests for <see cref="ExpenditureInsightPolicy"/>: the plain facts the dashboard states about one month's spending —
/// weekend against weekday daily average, the largest sub-class and its share, and the change from the month before.
/// October 2026 starts on a Thursday (9 weekend days, 22 weekdays); November 2026 starts on a Sunday and "today" is Saturday the 14th.
/// </summary>
public class ExpenditureInsightPolicyTests
{
    /// <summary>The account's "today" in these tests.</summary>
    private static readonly DateOnly _today = new(2026, 11, 14);

    /// <summary>A record of <paramref name="amount"/> in <paramref name="subClass"/> at noon of the given day.</summary>
    private static LocalExpenditure At(int month, int day, decimal amount, string subClass = "MealOrEatOutExpenses") =>
        new(new DateTime(2026, month, day, 12, 0, 0, DateTimeKind.Unspecified), subClass, amount);

    /// <summary>October: 900 on weekends (9 days, 100 a day) and 1,100 on weekdays (22 days, 50 a day); September 1,000.</summary>
    private static List<LocalExpenditure> October() =>
    [
        At(10, 3, 900), At(10, 1, 550, "HousingOrSuppliesCost"), At(10, 2, 550),
        At(9, 10, 1000),
    ];

    /// <summary>A weekend day costs twice a weekday on average: +100 %.</summary>
    [Fact]
    public void Insights_ComparesTheWeekendAndWeekdayDailyAverages()
    {
        var result = ExpenditureInsightPolicy.Insights(October(), 2026, 10, _today);

        Assert.Equal(new WeekendWeekdayComparison(100m, 50m, 100), result.WeekendWeekday);
    }

    /// <summary>A cheaper weekend gives a negative difference: 30 a weekend day against 50 a weekday is −40 %.</summary>
    [Fact]
    public void Insights_ACheaperWeekend_GivesANegativeDifference()
    {
        var result = ExpenditureInsightPolicy.Insights([At(10, 3, 270), At(10, 1, 1100)], 2026, 10, _today);

        Assert.Equal(new WeekendWeekdayComparison(30m, 50m, -40), result.WeekendWeekday);
    }

    /// <summary>With nothing spent on weekdays there is no ratio to state.</summary>
    [Fact]
    public void Insights_NothingSpentOnWeekdays_StatesNoComparison()
    {
        Assert.Null(ExpenditureInsightPolicy.Insights([At(10, 3, 900)], 2026, 10, _today).WeekendWeekday);
    }

    /// <summary>Before the month's first weekend (today is Thursday the 1st) there is no weekend average to compare.</summary>
    [Fact]
    public void Insights_BeforeTheFirstWeekend_StatesNoComparison()
    {
        Assert.Null(ExpenditureInsightPolicy.Insights([At(10, 1, 100)], 2026, 10, new DateOnly(2026, 10, 1)).WeekendWeekday);
    }

    /// <summary>The largest sub-class and its share of the month, rounded half away from zero (72.5 % → 73 %).</summary>
    [Fact]
    public void Insights_NamesTheLargestSubClassAndItsShare()
    {
        var result = ExpenditureInsightPolicy.Insights(October(), 2026, 10, _today);

        Assert.Equal(new SubClassShare("MealOrEatOutExpenses", 1450m, 73), result.TopSubClass);
    }

    /// <summary>A complete month is compared with the whole month before.</summary>
    [Fact]
    public void Insights_ACompleteMonth_IsComparedWithTheWholeMonthBefore()
    {
        var result = ExpenditureInsightPolicy.Insights(October(), 2026, 10, _today);

        Assert.Equal(new MonthOverMonthChange(1000m, 2000m, 100, false), result.MonthOverMonth);
    }

    /// <summary>
    /// The current month counts up to today only (a record dated later is not spent yet), and is compared with the same days
    /// of the month before: November 1–14 (4 weekend days, 10 weekdays) against October 1–14.
    /// </summary>
    [Fact]
    public void Insights_TheCurrentMonth_CountsUpToToday_AndIsComparedWithTheSameDaysBefore()
    {
        var result = ExpenditureInsightPolicy.Insights(
            [At(11, 1, 400), At(11, 2, 500), At(11, 20, 9999), At(10, 2, 300), At(10, 20, 9999)], 2026, 11, _today);

        Assert.Equal(new WeekendWeekdayComparison(100m, 50m, 100), result.WeekendWeekday);
        Assert.Equal(new MonthOverMonthChange(300m, 900m, 200, true), result.MonthOverMonth);
    }

    /// <summary>Without spending in the month before there is no change to state; without spending in the month, no largest class.</summary>
    [Fact]
    public void Insights_NothingToCompare_StatesNothing()
    {
        var result = ExpenditureInsightPolicy.Insights([At(9, 10, 1000)], 2026, 10, _today);

        Assert.Null(result.TopSubClass);
        Assert.Null(result.WeekendWeekday);
        Assert.Equal(new MonthOverMonthChange(1000m, 0m, -100, false), result.MonthOverMonth);
        Assert.Null(ExpenditureInsightPolicy.Insights(October(), 2026, 9, _today).MonthOverMonth);
    }

    /// <summary>A month that has not begun yet has no facts.</summary>
    [Fact]
    public void Insights_AFutureMonth_StatesNothing()
    {
        Assert.Equal(new ExpenditureInsights(null, null, null), ExpenditureInsightPolicy.Insights(October(), 2026, 12, _today));
    }
}
