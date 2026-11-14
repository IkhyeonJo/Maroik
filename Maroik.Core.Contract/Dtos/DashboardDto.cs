namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Aggregated data object used to populate the user dashboard page.
/// Carries the income and expenditure breakdowns and the asset data for the selected year and month.
/// </summary>
public class DashboardDto
{
    /// <summary>The user's preferred currency unit, used to filter displayed amounts.</summary>
    public string? DefaultMonetaryUnit { get; set; }

    /// <summary>Per-main-class income breakdown (subclass amounts + percentages), keyed by main class, for the selected year.</summary>
    public Dictionary<string, FinanceBreakdownDto> YearIncomeBreakdown { get; set; } = new();

    /// <summary>Per-main-class income breakdown for the selected year + month.</summary>
    public Dictionary<string, FinanceBreakdownDto> YearMonthIncomeBreakdown { get; set; } = new();

    /// <summary>Per-main-class expenditure breakdown for the selected year.</summary>
    public Dictionary<string, FinanceBreakdownDto> YearExpenditureBreakdown { get; set; } = new();

    /// <summary>Per-main-class expenditure breakdown for the selected year + month.</summary>
    public Dictionary<string, FinanceBreakdownDto> YearMonthExpenditureBreakdown { get; set; } = new();

    /// <summary>The page's charts: one per main class and period, each slice in the taxonomy's display order. Zeros when there is no default unit.</summary>
    public List<DashboardChartDto> Charts { get; set; } = [];

    /// <summary>The selected month's projected month-end spending in the default unit; null unless it is the current month and there is a basis.</summary>
    public DashboardForecastDto? Forecast { get; set; }

    /// <summary>The selected month's weekend against weekday spending in the default unit; null when it cannot be said.</summary>
    public DashboardWeekendWeekdayDto? WeekendWeekday { get; set; }

    /// <summary>The selected month's largest expenditure sub-class in the default unit; null for a month without spending.</summary>
    public DashboardTopSubClassDto? TopSubClass { get; set; }

    /// <summary>The selected month's spending against the month before in the default unit; null when that month spent nothing.</summary>
    public DashboardMonthOverMonthDto? MonthOverMonth { get; set; }

    /// <summary>All active assets owned by the user.</summary>
    public List<AssetResponse> Assets { get; set; } = [];

    /// <summary>Distinct currency units from the user's assets (used to populate the filter dropdown).</summary>
    public IEnumerable<string?> MonetaryUnits { get; set; } = [];

    /// <summary>Earliest year for which the user has income or expenditure data (used to build the year selector).</summary>
    public int StartYear { get; set; }

    /// <summary>Latest year for which the user has data.</summary>
    public int EndYear { get; set; }

    /// <summary>Currently selected year for filtering.</summary>
    public int SelectedYear { get; set; }

    /// <summary>Currently selected month (1–12) for filtering.</summary>
    public int SelectedMonth { get; set; }
}
