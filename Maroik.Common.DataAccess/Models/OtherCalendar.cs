using System;
using System.Collections.Generic;

namespace Maroik.Common.DataAccess.Models;

/// <summary>
/// Maroik.WebSite 달력
/// </summary>
public partial class OtherCalendar
{
    /// <summary>
    /// 계정 이메일 (ID)
    /// </summary>
    public string AccountEmail { get; set; }

    /// <summary>
    /// 부모 Calendar Id
    /// </summary>
    public int CalendarId { get; set; }

    public virtual Account AccountEmailNavigation { get; set; }
    
    public virtual Calendar Calendar { get; set; }
}