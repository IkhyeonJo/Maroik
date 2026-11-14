namespace Maroik.Core.Domain.Finances;

/// <summary>A month's spending against the month before.</summary>
/// <param name="PreviousTotal">The month before's spending over the compared days.</param>
/// <param name="CurrentTotal">The month's spending over the compared days.</param>
/// <param name="PercentChange">The change, in whole percent of <paramref name="PreviousTotal"/> (negative: less).</param>
/// <param name="SamePeriodOfMonth">True when only the same days of both months were compared (the current, unfinished month).</param>
public sealed record MonthOverMonthChange(decimal PreviousTotal, decimal CurrentTotal, int PercentChange, bool SamePeriodOfMonth);
