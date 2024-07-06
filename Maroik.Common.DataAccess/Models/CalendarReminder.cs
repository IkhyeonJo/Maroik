using System;

namespace Maroik.Common.DataAccess.Models;

/// <summary>
/// Maroik.WebSite 달력 알림
/// </summary>
public partial class CalendarReminder
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 부모 Calendar Event Id
    /// </summary>
    public int CalendarEventId { get; set; }

    /// <summary>
    /// 알림 방법
    /// </summary>
    public string Method { get; set; }

    /// <summary>
    /// 이벤트 전 알림 시간
    /// </summary>
    public TimeOnly TimeBeforeEvent { get; set; }

    public virtual CalendarEvent CalendarEvent { get; set; }
}
