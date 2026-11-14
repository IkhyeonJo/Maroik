using Maroik.Core.Domain.Finances;

namespace Maroik.Core.Domain.Tests.Finances;

/// <summary>
/// Unit tests for <see cref="ExpenditureForecastPolicy"/>: the month-end spending projected for the current month from what was
/// spent so far plus what the recent months spent after the same day of the month.
/// </summary>
public class ExpenditureForecastPolicyTests
{
    /// <summary>The account's "today" in these tests (a Saturday).</summary>
    private static readonly DateOnly _today = new(2026, 11, 14);

    /// <summary>A record of <paramref name="amount"/> at the given local time.</summary>
    private static LocalExpenditure At(int month, int day, decimal amount, int hour = 12, int minute = 0) =>
        new(new DateTime(2026, month, day, hour, minute, 0, DateTimeKind.Unspecified), "MealOrEatOutExpenses", amount);

    /// <summary>A month other than the current one is complete (or not yet begun): there is nothing to project.</summary>
    [Theory]
    [InlineData(2026, 10)]
    [InlineData(2026, 12)]
    [InlineData(2025, 11)]
    public void Forecast_ReturnsNull_ForAMonthOtherThanTheCurrentOne(int year, int month)
    {
        Assert.Null(ExpenditureForecastPolicy.Forecast([At(10, 20, 100), At(11, 1, 100)], year, month, _today));
    }

    /// <summary>Without a record in any of the recent months there is no pattern to project from.</summary>
    [Fact]
    public void Forecast_ReturnsNull_WithoutARecordInTheRecentMonths()
    {
        Assert.Null(ExpenditureForecastPolicy.Forecast([At(11, 1, 100), At(7, 30, 500)], 2026, 11, _today));
    }

    /// <summary>
    /// Spent so far counts this month's records up to the end of today (a record dated later is not spent yet). Each of the
    /// last <see cref="ExpenditureForecastPolicy.RecentMonths"/> months that has any record adds what it spent after day 14;
    /// their average is added to the spending so far. July is too far back, and August, with no record, is no basis.
    /// </summary>
    [Fact]
    public void Forecast_AddsTheRecentMonthsAverageSpendingAfterToday()
    {
        var result = ExpenditureForecastPolicy.Forecast(
        [
            At(11, 1, 100), At(11, 14, 50, 23, 59), At(11, 20, 999),
            At(10, 5, 30), At(10, 14, 40, 23, 59), At(10, 15, 200, 0),
            At(9, 15, 100),
            At(7, 20, 1000),
        ], 2026, 11, _today);

        Assert.Equal(3, ExpenditureForecastPolicy.RecentMonths);
        Assert.Equal(new ExpenditureForecast(150m, 300m, 2, null, 2), result);
    }

    /// <summary>
    /// A recent month whose records all fall before today's day still counts as a basis, with nothing after it; the average is
    /// rounded to the stored 4 decimal places.
    /// </summary>
    [Fact]
    public void Forecast_AMonthWithNothingAfterToday_CountsAsZero_AndTheAverageIsRounded()
    {
        var result = ExpenditureForecastPolicy.Forecast([At(10, 20, 100), At(9, 1, 5), At(8, 2, 5)], 2026, 11, _today);

        Assert.Equal(new ExpenditureForecast(0m, 33.3333m, 3, null, 2), result);
    }

    /// <summary>
    /// The typical error is the mean absolute error of the same projection made for each earlier month at the same day (a
    /// check of earlier months): with 100 on the 5th and 200 after the 14th, but 500 in September, July and August were projected exactly,
    /// September 300 short and October 100 over — (0 + 0 + 300 + 100) / 4 = 100. June has no month before it to project from.
    /// </summary>
    [Fact]
    public void Forecast_StatesTheTypicalError_OfTheSameProjectionInEarlierMonths()
    {
        List<LocalExpenditure> records = [];
        foreach ((int month, decimal after) in new[] { (6, 200m), (7, 200m), (8, 200m), (9, 500m), (10, 200m) })
        {
            records.Add(At(month, 5, 100));
            records.Add(At(month, 20, after));
        }
        records.Add(At(11, 5, 100));

        var result = ExpenditureForecastPolicy.Forecast(records, 2026, 11, _today);

        Assert.Equal(new ExpenditureForecast(100m, 400m, 3, 100m, 4), result);
    }

    /// <summary>Below <see cref="ExpenditureForecastPolicy.MinimumErrorMonths"/> earlier projections the typical error is not stated.</summary>
    [Fact]
    public void Forecast_StatesNoTypicalError_BelowTheMinimumOfEarlierProjections()
    {
        var result = ExpenditureForecastPolicy.Forecast([At(9, 5, 100), At(10, 5, 100), At(10, 20, 50), At(11, 1, 10)], 2026, 11, _today);

        Assert.Equal(3, ExpenditureForecastPolicy.MinimumErrorMonths);
        Assert.Equal(new ExpenditureForecast(10m, 35m, 2, null, 1), result);
    }
}
