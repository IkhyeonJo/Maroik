namespace Maroik.Core.Contract.Dtos;

public class CalendarSharedRequest
{
    public long CalendarId { get; set; }
    public bool User { get; set; }
    public bool Anonymous { get; set; }
}