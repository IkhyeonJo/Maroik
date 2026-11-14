namespace Maroik.Core.Domain.Finances;

/// <summary>Weekend against weekday spending of a month, each as an average per calendar day of its kind.</summary>
/// <param name="WeekendDailyAverage">Weekend spending divided by the number of Saturdays and Sundays counted.</param>
/// <param name="WeekdayDailyAverage">Weekday spending divided by the number of weekdays counted.</param>
/// <param name="PercentDifference">How much higher (negative: lower) the weekend average is, in whole percent of the weekday one.</param>
public sealed record WeekendWeekdayComparison(decimal WeekendDailyAverage, decimal WeekdayDailyAverage, int PercentDifference);
