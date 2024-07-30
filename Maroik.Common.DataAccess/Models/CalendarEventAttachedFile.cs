using System;

namespace Maroik.Common.DataAccess.Models;

/// <summary>
/// Maroik.WebSite 달력 이벤트 첨부파일
/// </summary>
public partial class CalendarEventAttachedFile
{
    /// <summary>
    /// PK
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 부모 CalendarEvent Id
    /// </summary>
    public int CalendarEventId { get; set; }

    /// <summary>
    /// 크기 (Byte)
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// 이름
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 확장자
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// 경로
    /// </summary>
    public string Path { get; set; }

    public virtual CalendarEvent CalendarEvent { get; set; }
}
