using System;

namespace Maroik.Common.DataAccess.Models;

/// <summary>
/// Maroik.WebSite 달력 할 일
/// </summary>
public partial class CalendarTask
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
    /// 할 일 제목
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 할 일 설명
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 마감 날짜 및 시간
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// 상태
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// 우선순위 (옵션)
    /// </summary>
    public string Priority { get; set; }

    public virtual Calendar Calendar { get; set; }
}
