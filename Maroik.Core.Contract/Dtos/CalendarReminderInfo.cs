namespace Maroik.Core.Contract.Dtos;

public class CalendarReminderInfo
{
    public string Method { get; init; } = string.Empty;
    public int? MinutesBeforeEvent { get; init; }
    public int? HoursBeforeEvent { get; init; }
    public int? DaysBeforeEvent { get; init; }
    public int? WeeksBeforeEvent { get; init; }
    public TimeSpan? TimesBeforeEvent { get; init; }
}
