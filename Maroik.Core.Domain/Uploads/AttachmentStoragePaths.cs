using Maroik.Core.Domain.Boards;

namespace Maroik.Core.Domain.Uploads;

/// <summary>
/// Where an uploaded file is stored. These are storage keys, not local paths: always "/"-separated, whatever OS this runs on. A stored
/// attachment is named by a GUID (only its extension is kept, the original name stays on its record), so names cannot collide or traverse paths.
/// </summary>
public static class AttachmentStoragePaths
{
    /// <summary>The storage directory of the avatars.</summary>
    public const string AvatarDirectory = "upload/Management/Profile/Avatar";

    /// <summary>A post's attachment: a private note under Management, everything else under Forum.</summary>
    public static string BoardAttachment(string boardType, long boardId, Guid id, string fileName)
    {
        string area = string.Equals(boardType, BoardTypes.PrivateNote, StringComparison.OrdinalIgnoreCase) ? "Management" : "Forum";
        return $"upload/{area}/{boardType}/boardAttachedFiles/{boardId}/{StoredName(id, fileName)}";
    }

    /// <summary>A calendar event's attachment, in the event's own folder.</summary>
    public static string CalendarEventAttachment(string roleIndex, long eventId, Guid id, string fileName) =>
        $"upload/Calendar/{roleIndex}/calendarEventAttachedFiles/{eventId}/{StoredName(id, fileName)}";

    /// <summary>An image pasted into a rich-text editor of a page.</summary>
    public static string EditorImage(string area, string subArea, string imageFile) => $"upload/{area}/{subArea}/summernote/images/{imageFile}";

    /// <summary>The upper-case GUID with the original extension.</summary>
    private static string StoredName(Guid id, string fileName) => $"{id.ToString().ToUpperInvariant()}{Path.GetExtension(fileName)}";
}
