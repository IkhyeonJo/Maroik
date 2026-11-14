namespace Maroik.Website.Models.ViewModels.Dashboards;

/// <summary>
/// Full output model for the Dashboard index page: the period and currency selectors and the charts, whose
/// amounts and percentages the service already computed for the selected year and year-month.
/// </summary>
public class UserIndexOutputViewModel
{
    /// <summary>Distinct currency codes present in the user's assets, for the currency-selector dropdown.</summary>
    public IEnumerable<string> MonetaryUnits { get; set; } = [];

    /// <summary>The currency code currently selected by the user for dashboard display.</summary>
    public string? DefaultMonetaryUnit { get; set; }

    // Year/Month selectors

    /// <summary>Earliest year for which the user has any income or expenditure data (lower bound of the year selector).</summary>
    public int StartYear { get; set; }

    /// <summary>Latest year with data (upper bound of the year selector).</summary>
    public int EndYear { get; set; }

    /// <summary>The year currently selected for the yearly and monthly breakdown views.</summary>
    public int SelectedYear { get; set; }

    /// <summary>The month (1–12) currently selected for the monthly breakdown view.</summary>
    public int SelectedMonth { get; set; }

    /// <summary>The charts of the page.</summary>
    public List<DashboardChartViewModel> Charts { get; set; } = [];

    /// <summary>Every sub-class the charts show once, so the view can render each one's localized label.</summary>
    public List<string> ChartSubClasses { get; set; } = [];

    /// <summary>
    /// The selected month's spending facts as localized sentences (projection, largest class, weekend against weekday, change
    /// from the month before), each present only when the service could state it. Empty: the view shows no card.
    /// </summary>
    public List<string> SpendingFacts { get; set; } = [];

    /// <summary>The fewest decimal places the chart tooltips show: the default currency's own (KRW 0, USD 2), or 0 when it is unknown.</summary>
    public int AmountMinDecimalPlaces { get; set; }

    /// <summary>The most decimal places the chart tooltips show: the default currency's own, or every stored place when it is unknown.</summary>
    public int AmountMaxDecimalPlaces { get; set; }
}
