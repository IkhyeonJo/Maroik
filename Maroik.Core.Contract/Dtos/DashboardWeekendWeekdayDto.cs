namespace Maroik.Core.Contract.Dtos;

/// <summary>The selected month's weekend against weekday daily average spending.</summary>
public class DashboardWeekendWeekdayDto
{
    /// <summary>Average spending per weekend day.</summary>
    public decimal WeekendDailyAverage { get; init; }

    /// <summary>Average spending per weekday.</summary>
    public decimal WeekdayDailyAverage { get; init; }

    /// <summary>How much higher (negative: lower) the weekend average is, in whole percent.</summary>
    public int PercentDifference { get; init; }
}
