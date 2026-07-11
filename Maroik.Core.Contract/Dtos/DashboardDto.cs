// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Aggregated data object used to populate the user dashboard page.
/// Carries income, expenditure, and asset data for the selected year and month.
/// </summary>
public class DashboardDto
{
    /// <summary>The user's preferred currency unit, used to filter displayed amounts.</summary>
    public string? DefaultMonetaryUnit { get; set; }

    /// <summary>All income records for the selected calendar year.</summary>
    public List<IncomeResponse> YearIncomes { get; set; } = [];

    /// <summary>All expenditure records for the selected calendar year.</summary>
    public List<ExpenditureResponse> YearExpenditures { get; set; } = [];

    /// <summary>Income records for the selected year + month.</summary>
    public List<IncomeResponse> YearMonthIncomes { get; set; } = [];

    /// <summary>Expenditure records for the selected year + month.</summary>
    public List<ExpenditureResponse> YearMonthExpenditures { get; set; } = [];

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
