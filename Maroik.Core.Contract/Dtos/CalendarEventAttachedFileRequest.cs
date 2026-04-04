namespace Maroik.Core.Contract.Dtos;

public class CalendarEventAttachedFileRequest
{
    public long Id { get; set; }
    public long CalendarEventId { get; set; }
    public long Size { get; set; }
    public string? Name { get; set; }
    public string? Extension { get; set; }
    public string? Path { get; set; }
}