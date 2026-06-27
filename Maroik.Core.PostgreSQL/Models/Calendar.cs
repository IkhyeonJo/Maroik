using System;
using System.Collections.Generic;

namespace Maroik.Core.PostgreSQL.Models;

/// <summary>
/// Calendar
/// </summary>
public partial class Calendar
{
    /// <summary>
    /// PK
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// AccountEmail (ID)
    /// </summary>
    public string? AccountEmail { get; set; }

    /// <summary>
    /// Name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// IANA TimeZone ID
    /// </summary>
    public string? TimeZoneIanaId { get; set; }

    /// <summary>
    /// HtmlColorCode
    /// </summary>
    public string? HtmlColorCode { get; set; }

    /// <summary>
    /// Created
    /// </summary>
    public DateTime Created { get; set; }

    /// <summary>
    /// Updated
    /// </summary>
    public DateTime Updated { get; set; }

    public virtual Account? AccountEmailNavigation { get; set; }

    public virtual ICollection<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

    public virtual CalendarShared? CalendarShared { get; set; }

    public virtual ICollection<Account> AccountEmails { get; set; } = new List<Account>();
    public virtual ICollection<OtherCalendar>? OtherCalendars { get; set; }
}
