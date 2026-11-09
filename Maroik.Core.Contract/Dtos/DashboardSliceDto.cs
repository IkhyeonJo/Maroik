namespace Maroik.Core.Contract.Dtos;

/// <summary>One sub-class of a dashboard chart.</summary>
public class DashboardSliceDto
{
    /// <summary>The sub-class (e.g. "LaborIncome").</summary>
    public string SubClass { get; set; } = "";

    /// <summary>Its summed amount.</summary>
    public decimal Amount { get; set; }

    /// <summary>Its share of the main class, 0-100.</summary>
    public double Percentage { get; set; }
}
