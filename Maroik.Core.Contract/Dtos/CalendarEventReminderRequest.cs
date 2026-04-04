namespace Maroik.Core.Contract.Dtos;

public class CalendarEventReminderRequest
{
    public long Id { get; set; }
    public long CalendarEventId { get; set; }
    public string? Method { get; set; }
    public long? MinutesBeforeEvent { get; set; }
    public long? HoursBeforeEvent { get; set; }
    public long? DaysBeforeEvent { get; set; }
    public long? WeeksBeforeEvent { get; set; }
    public TimeOnly? TimesBeforeEvent { get; set; }
}