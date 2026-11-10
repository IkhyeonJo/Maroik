namespace Maroik.Core.Domain.Uploads;

/// <summary>
/// What the storage keys of uploaded files share. These are storage keys, not local paths: always "/"-separated, whatever OS this
/// runs on. An attachment's own key is built by its context (<c>BoardAttachedFile.StoragePathFor</c>,
/// <c>CalendarEventAttachedFile.StoragePathFor</c>).
/// </summary>
public static class AttachmentStoragePaths
{
    /// <summary>The storage directory of the avatars.</summary>
    public const string AvatarDirectory = "upload/Management/Profile/Avatar";

    /// <summary>An image pasted into a rich-text editor of a page.</summary>
    public static string EditorImage(string area, string subArea, string imageFile) => $"upload/{area}/{subArea}/summernote/images/{imageFile}";

    /// <summary>
    /// The name a stored attachment gets: an upper-case GUID with the original extension. The original name goes no further (it stays on
    /// the attachment's record), so names cannot collide or traverse paths.
    /// </summary>
    public static string StoredName(Guid id, string fileName) => $"{id.ToString().ToUpperInvariant()}{Path.GetExtension(fileName)}";
}
