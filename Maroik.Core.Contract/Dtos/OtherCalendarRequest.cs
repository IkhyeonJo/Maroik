namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object used when a user subscribes to or unsubscribes from another user's shared calendar.
/// </summary>
public class OtherCalendarRequest
{
    /// <summary>Email of the subscriber account (composite primary key component).</summary>
    public string? AccountEmail { get; set; }

    /// <summary>ID of the calendar being subscribed to (composite primary key component).</summary>
    public long CalendarId { get; set; }
}
