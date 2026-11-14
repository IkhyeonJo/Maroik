using Maroik.Core.Contract.Dtos;
using Maroik.Core.Contract.Enums;
using Maroik.Website.Mappers;
using Maroik.Website.Models.ViewModels.Dashboards;

namespace Maroik.Website.Tests.Mappers;

/// <summary>
/// Unit tests for <see cref="DashboardViewModelMapper"/>. Which classes a chart shows and its numbers are the service's
/// (<c>DashboardServiceTests</c>, <c>FinanceBreakdownPolicyTests</c>): these tests only verify the mapper carries them over.
/// </summary>
public class DashboardViewModelMapperTests
{
    /// <summary>A localizer that returns its key — the keys are the English texts.</summary>
    private static string Identity(string key) => key;

    /// <summary>Maps <paramref name="dto"/> under the en-US culture (group separators and decimal point fixed).</summary>
    private static List<string> FactsOf(DashboardDto dto)
    {
        System.Globalization.CultureInfo before = System.Globalization.CultureInfo.CurrentCulture;
        System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        try
        {
            return dto.ToViewModel(Identity).SpendingFacts;
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = before;
        }
    }

    /// <summary>Without any fact there is no sentence (the view then shows no card).</summary>
    [Fact]
    public void ToViewModel_WithoutFacts_HasNoSpendingFacts()
    {
        Assert.Empty(FactsOf(BaseDto()));
    }

    /// <summary>
    /// The facts become short, plain sentences in a fixed order — projection, where most went, weekend against weekday, last
    /// month — with the amounts in the default currency's decimal places (KRW: none, so 3,200.5 is "3,201") and the sub-class
    /// through the localizer.
    /// </summary>
    [Fact]
    public void ToViewModel_StatesEveryFact_InOrder_InTheCurrencyDecimals()
    {
        DashboardDto dto = BaseDto();
        dto.Forecast = new DashboardForecastDto { SpentSoFar = 1450m, Projected = 3200.5m, BasisMonths = 3, AsOf = new DateOnly(2025, 6, 14) };
        dto.TopSubClass = new DashboardTopSubClassDto { SubClass = "MealOrEatOutExpenses", Amount = 1450m, Percentage = 73 };
        dto.WeekendWeekday = new DashboardWeekendWeekdayDto { WeekendDailyAverage = 78533.3333m, WeekdayDailyAverage = 166952.381m, PercentDifference = 100 };
        dto.MonthOverMonth = new DashboardMonthOverMonthDto { PreviousTotal = 1000m, CurrentTotal = 2000m, PercentChange = 100, SamePeriodOfMonth = false };

        Assert.Equal(
        [
            "Expected spending this month (as of 2025-06-14): about 3,201 KRW (so far 1,450 KRW, based on the last 3 month(s))",
            "Most spent on: MealOrEatOutExpenses (73%, 1,450 KRW)",
            "Average per weekend day 78,533 KRW, per weekday 166,952 KRW (100% more on weekends)",
            "100% more than last month (1,000 → 2,000 KRW)",
        ], FactsOf(dto));
    }

    /// <summary>A currency with decimals keeps them, two for USD (12.5 is "12.50").</summary>
    [Fact]
    public void ToViewModel_Facts_ShowAUsdAmountWithTwoDecimals()
    {
        DashboardDto dto = BaseDto();
        dto.DefaultMonetaryUnit = "USD";
        dto.TopSubClass = new DashboardTopSubClassDto { SubClass = "MealOrEatOutExpenses", Amount = 12.5m, Percentage = 100 };

        Assert.Equal(["Most spent on: MealOrEatOutExpenses (100%, 12.50 USD)"], FactsOf(dto));
    }

    /// <summary>
    /// Once measured, the projection says how far off such a projection usually is, so it reads as an estimate, not as a figure
    /// the month will reach.
    /// </summary>
    [Fact]
    public void ToViewModel_ForecastSentence_StatesTheUsualError_WhenMeasured()
    {
        DashboardDto dto = BaseDto();
        dto.Forecast = new DashboardForecastDto { SpentSoFar = 100m, Projected = 400m, BasisMonths = 3, TypicalError = 1234.5m, ErrorMonths = 4, AsOf = new DateOnly(2025, 6, 14) };

        Assert.Equal(
            ["Expected spending this month (as of 2025-06-14): about 400 KRW (so far 100 KRW, based on the last 3 month(s), usually off by about 1,235 KRW)"],
            FactsOf(dto));
    }

    /// <summary>Weekends that cost less say "less" with the size of the difference; equal ones say "about the same".</summary>
    [Theory]
    [InlineData(-40, "Average per weekend day 30 KRW, per weekday 50 KRW (40% less on weekends)")]
    [InlineData(0, "Average per weekend day 30 KRW, per weekday 50 KRW (about the same)")]
    public void ToViewModel_WeekendSentence_FollowsTheSign(int percent, string expected)
    {
        DashboardDto dto = BaseDto();
        dto.WeekendWeekday = new DashboardWeekendWeekdayDto { WeekendDailyAverage = 30m, WeekdayDailyAverage = 50m, PercentDifference = percent };

        Assert.Equal([expected], FactsOf(dto));
    }

    /// <summary>
    /// Less spending says "less" with the size of the change, no change "about the same", and the current month names the same
    /// days of last month instead of the whole of it.
    /// </summary>
    [Theory]
    [InlineData(-25, false, "25% less than last month (1,000 → 750 KRW)")]
    [InlineData(0, true, "About the same as the same days last month (1,000 → 750 KRW)")]
    [InlineData(10, true, "10% more than the same days last month (1,000 → 750 KRW)")]
    public void ToViewModel_MonthOverMonthSentence_FollowsTheSignAndThePeriod(int percent, bool samePeriod, string expected)
    {
        DashboardDto dto = BaseDto();
        dto.MonthOverMonth = new DashboardMonthOverMonthDto { PreviousTotal = 1000m, CurrentTotal = 750m, PercentChange = percent, SamePeriodOfMonth = samePeriod };

        Assert.Equal([expected], FactsOf(dto));
    }

    /// <summary>
    /// The chart tooltips get the default currency's decimal places as their least and most digits (KRW 0–0, USD 2–2); an
    /// unknown currency or none shows every stored place (0–4).
    /// </summary>
    [Theory]
    [InlineData("KRW", 0, 0)]
    [InlineData("USD", 2, 2)]
    [InlineData("$", 0, 4)]
    [InlineData(null, 0, 4)]
    public void ToViewModel_GivesTheChartsTheDefaultCurrencyDecimals(string? currency, int least, int most)
    {
        DashboardDto dto = BaseDto();
        dto.DefaultMonetaryUnit = currency;

        UserIndexOutputViewModel vm = dto.ToViewModel(Identity);

        Assert.Equal((least, most), (vm.AmountMinDecimalPlaces, vm.AmountMaxDecimalPlaces));
    }

    /// <summary>A dashboard DTO with fixed selectors and no charts, to be adjusted per test.</summary>
    private static DashboardDto BaseDto() => new()
    {
        DefaultMonetaryUnit = "KRW",
        Assets = [],
        MonetaryUnits = ["KRW", null, "USD"],
        StartYear = 2020,
        EndYear = 2025,
        SelectedYear = 2025,
        SelectedMonth = 6
    };

    /// <summary>A chart of the given main class and period.</summary>
    private static DashboardChartDto Chart(string mainClass, DashboardPeriod period, params (string SubClass, decimal Amount, double Percentage)[] slices) => new()
    {
        MainClass = mainClass,
        Period = period,
        Slices = [.. slices.Select(s => new DashboardSliceDto { SubClass = s.SubClass, Amount = s.Amount, Percentage = s.Percentage })]
    };

    /// <summary>The selectors and the default unit are copied.</summary>
    [Fact]
    public void ToViewModel_CopiesTheSelectorsAndTheDefaultUnit()
    {
        UserIndexOutputViewModel vm = BaseDto().ToViewModel(Identity);

        Assert.Equal((2020, 2025, 2025, 6, "KRW"), (vm.StartYear, vm.EndYear, vm.SelectedYear, vm.SelectedMonth, vm.DefaultMonetaryUnit));
    }

    /// <summary>A null unit is no option of the currency selector.</summary>
    [Fact]
    public void ToViewModel_FiltersNullMonetaryUnits()
    {
        Assert.Equal(["KRW", "USD"], BaseDto().ToViewModel(Identity).MonetaryUnits);
    }

    /// <summary>A chart is drawn on the canvas of its main class and period, with its slices in the order the service gave.</summary>
    [Theory]
    [InlineData("RegularIncome", DashboardPeriod.Year, "regularIncomeYear")]
    [InlineData("ConsumerSpending", DashboardPeriod.YearMonth, "consumerSpendingYearMonth")]
    public void ToViewModel_CarriesEachChart_ToItsCanvas(string mainClass, DashboardPeriod period, string canvasId)
    {
        DashboardDto dto = BaseDto();
        dto.Charts = [Chart(mainClass, period, ("B", 2.5m, 25.0), ("A", 7.5m, 75.0))];

        DashboardChartViewModel chart = Assert.Single(dto.ToViewModel(Identity).Charts);

        Assert.Equal(canvasId, chart.CanvasId);
        Assert.Equal(["B", "A"], chart.SubClasses);
        Assert.Equal([2.5m, 7.5m], chart.Amounts);
        Assert.Equal([25.0, 75.0], chart.Percentages);
    }

    /// <summary>The view renders one label per sub-class, however many charts show it.</summary>
    [Fact]
    public void ToViewModel_ListsEachSubClassOnce()
    {
        DashboardDto dto = BaseDto();
        dto.Charts = [Chart("RegularIncome", DashboardPeriod.Year, ("LaborIncome", 1m, 100.0)), Chart("IrregularIncome", DashboardPeriod.Year, ("LaborIncome", 1m, 50.0), ("OtherIncome", 1m, 50.0))];

        Assert.Equal(["LaborIncome", "OtherIncome"], dto.ToViewModel(Identity).ChartSubClasses);
    }
}
