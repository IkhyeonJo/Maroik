using ErrorOr;
using Maroik.Core.Domain.Errors;
using Maroik.Core.Domain.Primitives;
using Maroik.Core.Domain.Uploads;

namespace Maroik.Core.Domain.Boards;

/// <summary>
/// Aggregate root representing a file attached to a <see cref="Board"/> post.
/// Persisted independently via its own <c>IBoardAttachedFileRepository</c> (not hydrated or
/// cascaded through the <see cref="Board"/> aggregate), which is why it is an aggregate root
/// rather than a child entity; it references its post by <see cref="BoardId"/> only.
/// </summary>
public sealed class BoardAttachedFile : AggregateRoot<long>
{
    /// <summary>ID of the parent board post.</summary>
    public long BoardId { get; }

    /// <summary>File size in bytes.</summary>
    public long Size { get; }

    /// <summary>Original file name (e.g. "report.zip").</summary>
    public string Name { get; }

    /// <summary>File extension including the dot (e.g. ".zip"). Null when there is no extension.</summary>
    public string? Extension { get; }

    /// <summary>Server-side storage path where the file is saved.</summary>
    public string Path { get; }

    /// <summary>Sets every field; reached only through <see cref="Reconstitute"/> / <see cref="Create"/>.</summary>
    private BoardAttachedFile(
        long id,
        long boardId,
        long size,
        string name,
        string? extension,
        string path) : base(id)
    {
        BoardId = boardId;
        Size = size;
        Name = name;
        Extension = extension;
        Path = path;
    }

    /// <summary>The storage key of a post's attachment: a private note under Management, every other post under Forum.</summary>
    public static string StoragePathFor(string boardType, long boardId, Guid id, string fileName)
    {
        string area = string.Equals(boardType, BoardTypes.PrivateNote, StringComparison.OrdinalIgnoreCase) ? "Management" : "Forum";
        return $"upload/{area}/{boardType}/boardAttachedFiles/{boardId}/{AttachmentStoragePaths.StoredName(id, fileName)}";
    }

    /// <summary>Whether a file is attached: an edit that removes the attachment empties the record (see <see cref="Cleared"/>) instead of deleting it.</summary>
    public bool HasFile => !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Path);

    /// <summary>
    /// Rebuilds a <see cref="BoardAttachedFile"/> from trusted data from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// </summary>
    public static BoardAttachedFile Reconstitute(
        long id, long boardId, long size, string? name, string? extension, string? path)
        => new(id, boardId, size, name ?? "", extension, path ?? "");

    /// <summary>
    /// This record with no file: size 0 and empty name, extension and path, the empty-attachment values a post's record holds once
    /// its file was removed. They are not user input, so <see cref="Create"/> (which refuses an empty name or path) does not apply.
    /// </summary>
    public BoardAttachedFile Cleared() => new(Id, BoardId, 0, "", "", "");

    /// <summary>This record, under its own id, holding the file of the already validated <paramref name="replacement"/>.</summary>
    public BoardAttachedFile ReplacedBy(BoardAttachedFile replacement) =>
        new(Id, replacement.BoardId, replacement.Size, replacement.Name, replacement.Extension, replacement.Path);

    /// <summary>Creates a new attached-file entity after validating required fields.</summary>
    public static ErrorOr<BoardAttachedFile> Create(
        long boardId,
        long size,
        string? name,
        string? extension,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(name))
            return DomainError.Validation("BoardAttachedFile.NameEmpty", "File name cannot be empty.");

        if (string.IsNullOrWhiteSpace(path))
            return DomainError.Validation("BoardAttachedFile.PathEmpty", "File storage path cannot be empty.");

        if (size < 0)
            return DomainError.Validation("BoardAttachedFile.InvalidSize", "File size cannot be negative.");

        // Name, Extension and Path are each persisted in a character varying(255) column: reject an
        // over-long value here (a clean validation error, before anything is uploaded) instead of a raw
        // "value too long" (SQLSTATE 22001) from the write.
        if (name.Length > ShortTextPolicy.MaxLength)
            return DomainError.Validation("BoardAttachedFile.NameTooLong", "File name must be {0} characters or fewer.", ShortTextPolicy.MaxLength);

        if (extension?.Length > ShortTextPolicy.MaxLength)
            return DomainError.Validation("BoardAttachedFile.ExtensionTooLong", "File extension must be {0} characters or fewer.", ShortTextPolicy.MaxLength);

        if (path.Length > ShortTextPolicy.MaxLength)
            return DomainError.Validation("BoardAttachedFile.PathTooLong", "File storage path must be {0} characters or fewer.", ShortTextPolicy.MaxLength);

        return new BoardAttachedFile(0, boardId, size, name, extension, path);
    }
}
