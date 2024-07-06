using System.Collections.Generic;

namespace Maroik.Common.DataAccess.Models;

/// <summary>
/// Maroik.WebSite 달력
/// </summary>
public partial class Calendar
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 계정 이메일 (ID)
    /// </summary>
    public string AccountEmail { get; set; }

    /// <summary>
    /// 이름
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// IANA TimeZone ID
    /// </summary>
    public string TimeZoneIanaId { get; set; }

    public virtual Account AccountEmailNavigation { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = [];

    public virtual ICollection<CalendarTask> CalendarTasks { get; set; } = [];
}
