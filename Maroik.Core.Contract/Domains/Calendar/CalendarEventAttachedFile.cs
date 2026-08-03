using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Calendar;

/// <summary>
/// Entity representing a file attached to a <see cref="CalendarEvent"/>.
/// Owned by the <see cref="CalendarEvent"/> aggregate root.
/// </summary>
public sealed class CalendarEventAttachedFile : Entity<long>
{
    /// <summary>ID of the parent calendar event.</summary>
    public long CalendarEventId { get; private set; }

    /// <summary>File size in bytes.</summary>
    public long Size { get; private set; }

    /// <summary>Original file name (e.g. "agenda.pdf").</summary>
    public string Name { get; private set; }

    /// <summary>File extension including the dot (e.g. ".pdf"). Null when there is no extension.</summary>
    public string? Extension { get; private set; }

    /// <summary>Server-side storage path of the file.</summary>
    public string Path { get; private set; }

    private CalendarEventAttachedFile(
        long id,
        long calendarEventId,
        long size,
        string name,
        string? extension,
        string path) : base(id)
    {
        CalendarEventId = calendarEventId;
        Size = size;
        Name = name;
        Extension = extension;
        Path = path;
    }

    /// <summary>
    /// Rebuilds a <see cref="CalendarEventAttachedFile"/> from trusted data from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// </summary>
    public static CalendarEventAttachedFile Reconstitute(
        long id, long calendarEventId, long size, string? name, string? extension, string? path)
        => new(id, calendarEventId, size, name ?? "", extension, path ?? "");

    /// <summary>Creates a new attached-file entity after validating required fields.</summary>
    public static ErrorOr<CalendarEventAttachedFile> Create(
        long calendarEventId,
        long size,
        string? name,
        string? extension,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("CalendarEventAttachedFile.NameEmpty", "File name cannot be empty.");

        if (string.IsNullOrWhiteSpace(path))
            return Error.Validation("CalendarEventAttachedFile.PathEmpty", "File storage path cannot be empty.");

        if (size < 0)
            return Error.Validation("CalendarEventAttachedFile.InvalidSize", "File size cannot be negative.");

        return new CalendarEventAttachedFile(0, calendarEventId, size, name, extension, path);
    }
}
