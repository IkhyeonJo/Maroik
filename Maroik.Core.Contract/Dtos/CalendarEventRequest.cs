namespace Maroik.Core.Contract.Dtos;

public class CalendarEventRequest
{
    public long Id { get; set; }
    public long CalendarId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public bool AllDay { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? StartDateTimeZoneIanaId { get; set; }
    public string? EndDateTimeZoneIanaId { get; set; }
    public string? Location { get; set; }
    public string? Status { get; set; }
    public long? RecurrenceId { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}