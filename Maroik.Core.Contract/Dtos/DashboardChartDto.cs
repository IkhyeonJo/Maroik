using Maroik.Core.Contract.Enums;

namespace Maroik.Core.Contract.Dtos;

/// <summary>One chart of the dashboard: a main class in one period.</summary>
public class DashboardChartDto
{
    /// <summary>The main class (e.g. "RegularIncome").</summary>
    public string MainClass { get; set; } = "";

    /// <summary>The period.</summary>
    public DashboardPeriod Period { get; set; }

    /// <summary>Every sub-class of the main class, in the taxonomy's display order.</summary>
    public List<DashboardSliceDto> Slices { get; set; } = [];
}
