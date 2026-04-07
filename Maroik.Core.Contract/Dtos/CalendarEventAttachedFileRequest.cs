namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object used to save or update a file attached to a calendar event.
/// </summary>
public class CalendarEventAttachedFileRequest
{
    /// <summary>Attached file ID (auto-incremented primary key).</summary>
    public long Id { get; set; }

    /// <summary>ID of the parent calendar event (foreign key).</summary>
    public long CalendarEventId { get; set; }

    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Original file name.</summary>
    public string? Name { get; set; }

    /// <summary>File extension (e.g. ".pdf", ".jpg"). Null if none.</summary>
    public string? Extension { get; set; }

    /// <summary>Server-side storage path of the file.</summary>
    public string? Path { get; set; }
}
