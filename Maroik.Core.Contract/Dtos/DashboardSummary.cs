namespace Maroik.Core.Contract.Dtos;

public class DashboardSummary
{
    public string? DefaultMonetaryUnit { get; set; }
    public List<IncomeResponse> YearIncomes { get; set; } = [];
    public List<ExpenditureResponse> YearExpenditures { get; set; } = [];
    public List<IncomeResponse> YearMonthIncomes { get; set; } = [];
    public List<ExpenditureResponse> YearMonthExpenditures { get; set; } = [];
    public List<AssetResponse> Assets { get; set; } = [];
    public IEnumerable<string?> MonetaryUnits { get; set; } = [];
    public int StartYear { get; set; }
    public int EndYear { get; set; }
    public int SelectedYear { get; set; }
    public int SelectedMonth { get; set; }
}
