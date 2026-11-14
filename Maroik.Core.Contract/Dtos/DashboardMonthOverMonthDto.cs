namespace Maroik.Core.Contract.Dtos;

/// <summary>The selected month's spending against the month before.</summary>
public class DashboardMonthOverMonthDto
{
    /// <summary>The month before's spending over the compared days.</summary>
    public decimal PreviousTotal { get; init; }

    /// <summary>The month's spending over the compared days.</summary>
    public decimal CurrentTotal { get; init; }

    /// <summary>The change in whole percent (negative: less).</summary>
    public int PercentChange { get; init; }

    /// <summary>True when only the same days of both months were compared (the current, unfinished month).</summary>
    public bool SamePeriodOfMonth { get; init; }
}
