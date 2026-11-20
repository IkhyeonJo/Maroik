using Maroik.Core.Contract.Dtos;
using Maroik.Core.Service.Constants;
using Maroik.Website.Constants;
using Maroik.Website.Models.ViewModels.Dashboards;

namespace Maroik.Website.Mappers;

/// <summary>
/// Converts a <see cref="DashboardDto"/> returned by <c>IDashboardService</c> into the
/// <see cref="UserIndexOutputViewModel"/> expected by the Dashboard view. Response-to-view-model mapping
/// only: which classes a chart shows and every number on it come from the service.
/// </summary>
public static class DashboardViewModelMapper
{
    extension(DashboardDto summary)
    {
        /// <summary>Maps the summary to a fully-populated <see cref="UserIndexOutputViewModel"/>.</summary>
        public UserIndexOutputViewModel ToViewModel() => new()
        {
            DefaultMonetaryUnit = summary.DefaultMonetaryUnit,
            MonetaryUnits = summary.MonetaryUnits.Where(x => x != null).Select(x => x!),
            StartYear = summary.StartYear,
            EndYear = summary.EndYear,
            SelectedYear = summary.SelectedYear,
            SelectedMonth = summary.SelectedMonth,
            Charts = [.. summary.Charts.Select(ToViewModel)],
            ChartSubClasses = [.. summary.Charts.SelectMany(c => c.Slices).Select(s => s.SubClass).Distinct()],
            AmountMinDecimalPlaces = AmountDisplay.DecimalPlacesOf(summary.DefaultMonetaryUnit) ?? 0,
            AmountMaxDecimalPlaces = AmountDisplay.DecimalPlacesOf(summary.DefaultMonetaryUnit) ?? PolicyLimits.AmountMaxDecimalPlaces
        };
    }

    /// <summary>A chart for the canvas of its main class and period (e.g. <c>regularIncomeYear</c>).</summary>
    private static DashboardChartViewModel ToViewModel(DashboardChartDto chart) => new()
    {
        CanvasId = $"{char.ToLowerInvariant(chart.MainClass[0])}{chart.MainClass[1..]}{chart.Period}",
        SubClasses = [.. chart.Slices.Select(s => s.SubClass)],
        Amounts = [.. chart.Slices.Select(s => s.Amount)],
        Percentages = [.. chart.Slices.Select(s => s.Percentage)]
    };
}
