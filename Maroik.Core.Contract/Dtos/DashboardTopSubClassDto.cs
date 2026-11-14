namespace Maroik.Core.Contract.Dtos;

/// <summary>The selected month's sub-class with the most spending.</summary>
public class DashboardTopSubClassDto
{
    /// <summary>The sub-class, as stored (the resx key of its label).</summary>
    public required string SubClass { get; init; }

    /// <summary>Its spending in the month.</summary>
    public decimal Amount { get; init; }

    /// <summary>Its share of the month's spending, in whole percent.</summary>
    public int Percentage { get; init; }
}
