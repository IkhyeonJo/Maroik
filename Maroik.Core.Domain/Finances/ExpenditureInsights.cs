namespace Maroik.Core.Domain.Finances;

/// <summary>The facts the dashboard states about one month's spending (<see cref="ExpenditureInsightPolicy"/>); each is null when it cannot be said.</summary>
/// <param name="WeekendWeekday">Weekend against weekday daily average.</param>
/// <param name="TopSubClass">The sub-class with the most spending and its share.</param>
/// <param name="MonthOverMonth">The change from the month before.</param>
public sealed record ExpenditureInsights(WeekendWeekdayComparison? WeekendWeekday, SubClassShare? TopSubClass, MonthOverMonthChange? MonthOverMonth);
