namespace Maroik.Core.PostgreSQL.Models;

/// <summary>
/// OtherCalendar
/// </summary>
public partial class OtherCalendar
{
    /// <summary>
    /// Account Email (ID)
    /// </summary>
    public string? AccountEmail { get; set; }

    /// <summary>
    /// Parent Calendar Id
    /// </summary>
    public long CalendarId { get; set; }

    public virtual Account Account { get; set; } = new Account();

    public virtual Calendar Calendar { get; set; } = new Calendar();
}