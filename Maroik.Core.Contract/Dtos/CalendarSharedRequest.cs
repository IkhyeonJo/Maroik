namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object used to set the sharing permissions of a calendar.
/// </summary>
public class CalendarSharedRequest
{
    /// <summary>ID of the calendar whose sharing settings are being updated (primary key).</summary>
    public long CalendarId { get; set; }

    /// <summary>When true, registered users can view this calendar.</summary>
    public bool User { get; set; }

    /// <summary>When true, anonymous (unauthenticated) visitors can view this calendar.</summary>
    public bool Anonymous { get; set; }
}
