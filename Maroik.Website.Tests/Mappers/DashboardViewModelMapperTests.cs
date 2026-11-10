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
        UserIndexOutputViewModel vm = BaseDto().ToViewModel();

        Assert.Equal((2020, 2025, 2025, 6, "KRW"), (vm.StartYear, vm.EndYear, vm.SelectedYear, vm.SelectedMonth, vm.DefaultMonetaryUnit));
    }

    /// <summary>A null unit is no option of the currency selector.</summary>
    [Fact]
    public void ToViewModel_FiltersNullMonetaryUnits()
    {
        Assert.Equal(["KRW", "USD"], BaseDto().ToViewModel().MonetaryUnits);
    }

    /// <summary>A chart is drawn on the canvas of its main class and period, with its slices in the order the service gave.</summary>
    [Theory]
    [InlineData("RegularIncome", DashboardPeriod.Year, "regularIncomeYear")]
    [InlineData("ConsumerSpending", DashboardPeriod.YearMonth, "consumerSpendingYearMonth")]
    public void ToViewModel_CarriesEachChart_ToItsCanvas(string mainClass, DashboardPeriod period, string canvasId)
    {
        DashboardDto dto = BaseDto();
        dto.Charts = [Chart(mainClass, period, ("B", 2.5m, 25.0), ("A", 7.5m, 75.0))];

        DashboardChartViewModel chart = Assert.Single(dto.ToViewModel().Charts);

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

        Assert.Equal(["LaborIncome", "OtherIncome"], dto.ToViewModel().ChartSubClasses);
    }
}
