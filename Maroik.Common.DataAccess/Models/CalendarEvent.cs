using System;
using System.Collections.Generic;

namespace Maroik.Common.DataAccess.Models;

/// <summary>
/// Maroik.WebSite 달력 이벤트
/// </summary>
public partial class CalendarEvent
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 부모 Calendar Id
    /// </summary>
    public int CalendarId { get; set; }

    /// <summary>
    /// 제목
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 설명
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 시작 날짜 및 시간
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 종료 날짜 및 시간
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 시작 날짜 및 시간 (IANA TimeZone ID)
    /// </summary>
    public string StartDateTimeZoneIanaId { get; set; }

    /// <summary>
    /// 종료 날짜 및 시간 (IANA TimeZone ID)
    /// </summary>
    public string EndDateTimeZoneIanaId { get; set; }

    /// <summary>
    /// 위치
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// 반복 규칙 ID (옵션)
    /// </summary>
    public int? RecurrenceId { get; set; }

    public virtual Calendar Calendar { get; set; }

    public virtual ICollection<CalendarEventAttachedFile> CalendarEventAttachedFiles { get; set; } = [];

    public virtual ICollection<CalendarReminder> CalendarReminders { get; set; } = [];

    public virtual CalendarRecurrence Recurrence { get; set; }
}
