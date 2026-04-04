namespace Maroik.Core.Contract.Dtos;

public class CalendarResponse
{
    public long Id { get; set; }
    public string? AccountEmail { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? TimeZoneIanaId { get; set; }
    public string? HtmlColorCode { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}