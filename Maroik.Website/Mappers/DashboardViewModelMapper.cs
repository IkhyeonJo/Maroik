using System.Globalization;
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
        /// <summary>Maps the summary to a fully-populated <see cref="UserIndexOutputViewModel"/>; <paramref name="localize"/> words the spending facts.</summary>
        public UserIndexOutputViewModel ToViewModel(Func<string, string> localize) => new()
        {
            DefaultMonetaryUnit = summary.DefaultMonetaryUnit,
            MonetaryUnits = summary.MonetaryUnits.Where(x => x != null).Select(x => x!),
            StartYear = summary.StartYear,
            EndYear = summary.EndYear,
            SelectedYear = summary.SelectedYear,
            SelectedMonth = summary.SelectedMonth,
            Charts = [.. summary.Charts.Select(ToViewModel)],
            ChartSubClasses = [.. summary.Charts.SelectMany(c => c.Slices).Select(s => s.SubClass).Distinct()],
            SpendingFacts = SpendingFactsOf(summary, localize),
            AmountMinDecimalPlaces = AmountDisplay.DecimalPlacesOf(summary.DefaultMonetaryUnit) ?? 0,
            AmountMaxDecimalPlaces = AmountDisplay.DecimalPlacesOf(summary.DefaultMonetaryUnit) ?? PolicyLimits.AmountMaxDecimalPlaces
        };
    }

    /// <summary>
    /// The spending facts the service stated, worded in short, plain sentences in a fixed order: projection, where most went,
    /// weekend against weekday, last month. Only the wording is chosen here (which template fits the sign of a number); every
    /// number is the service's, shown in the default currency's decimal places (<see cref="AmountDisplay.Text"/>).
    /// </summary>
    private static List<string> SpendingFactsOf(DashboardDto summary, Func<string, string> localize)
    {
        string unit = summary.DefaultMonetaryUnit ?? "";
        List<string> facts = [];

        if (summary.Forecast is { } forecast)
        {
            // The date and, once measured, the usual error say what the figure is: an estimate made on that day, about this far off.
            string asOf = forecast.AsOf.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            facts.Add(forecast.TypicalError is { } error
                ? Format(localize, "Expected spending this month (as of {4}): about {0} {3} (so far {1} {3}, based on the last {2} month(s), usually off by about {5} {3})",
                    Amount(forecast.Projected, unit), Amount(forecast.SpentSoFar, unit), forecast.BasisMonths, unit, asOf, Amount(error, unit))
                : Format(localize, "Expected spending this month (as of {4}): about {0} {3} (so far {1} {3}, based on the last {2} month(s))",
                    Amount(forecast.Projected, unit), Amount(forecast.SpentSoFar, unit), forecast.BasisMonths, unit, asOf));
        }

        if (summary.TopSubClass is { } top)
            facts.Add(Format(localize, "Most spent on: {0} ({1}%, {2} {3})", localize(top.SubClass), top.Percentage, Amount(top.Amount, unit), unit));

        if (summary.WeekendWeekday is { } week)
        {
            string template = week.PercentDifference switch
            {
                > 0 => "Average per weekend day {1} {3}, per weekday {2} {3} ({0}% more on weekends)",
                < 0 => "Average per weekend day {1} {3}, per weekday {2} {3} ({0}% less on weekends)",
                _ => "Average per weekend day {1} {3}, per weekday {2} {3} (about the same)"
            };
            facts.Add(Format(localize, template,
                Math.Abs(week.PercentDifference), Amount(week.WeekendDailyAverage, unit), Amount(week.WeekdayDailyAverage, unit), unit));
        }

        if (summary.MonthOverMonth is { } change)
        {
            string template = change.PercentChange switch
            {
                > 0 => "{0}% more than {1} ({2} → {3} {4})",
                < 0 => "{0}% less than {1} ({2} → {3} {4})",
                _ => "About the same as {1} ({2} → {3} {4})"
            };
            string period = localize(change.SamePeriodOfMonth ? "the same days last month" : "last month");
            facts.Add(Format(localize, template,
                Math.Abs(change.PercentChange), period, Amount(change.PreviousTotal, unit), Amount(change.CurrentTotal, unit), unit));
        }

        return facts;
    }

    /// <summary>The localized <paramref name="template"/> filled with <paramref name="args"/> in the current culture.</summary>
    private static string Format(Func<string, string> localize, string template, params object[] args) =>
        string.Format(CultureInfo.CurrentCulture, localize(template), args);

    /// <summary><paramref name="amount"/> as the screens show it in <paramref name="currency"/> (<see cref="AmountDisplay.Text"/>).</summary>
    private static string Amount(decimal amount, string currency) => AmountDisplay.Text(amount, currency);

    /// <summary>A chart for the canvas of its main class and period (e.g. <c>regularIncomeYear</c>).</summary>
    private static DashboardChartViewModel ToViewModel(DashboardChartDto chart) => new()
    {
        CanvasId = $"{char.ToLowerInvariant(chart.MainClass[0])}{chart.MainClass[1..]}{chart.Period}",
        SubClasses = [.. chart.Slices.Select(s => s.SubClass)],
        Amounts = [.. chart.Slices.Select(s => s.Amount)],
        Percentages = [.. chart.Slices.Select(s => s.Percentage)]
    };
}
