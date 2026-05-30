using ErrorOr;
using Maroik.Core.Contract.Domains.Primitives;

namespace Maroik.Core.Contract.Domains.Board;

/// <summary>
/// Entity representing a file attached to a <see cref="Board"/> post.
/// Owned by the <see cref="Board"/> aggregate root.
/// </summary>
public sealed class BoardAttachedFile : Entity<long>
{
    /// <summary>ID of the parent board post.</summary>
    public long BoardId { get; private set; }

    /// <summary>File size in bytes.</summary>
    public long Size { get; private set; }

    /// <summary>Original file name (e.g. "report.pdf").</summary>
    public string Name { get; private set; }

    /// <summary>File extension including the dot (e.g. ".pdf"). Null when there is no extension.</summary>
    public string? Extension { get; private set; }

    /// <summary>Server-side storage path where the file is saved.</summary>
    public string Path { get; private set; }

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

    /// <summary>
    /// Rebuilds a <see cref="BoardAttachedFile"/> from trusted data from the repository layer.
    /// Bypasses validation — use only with data from the trusted repository layer.
    /// </summary>
    public static BoardAttachedFile Reconstitute(
        long id, long boardId, long size, string? name, string? extension, string? path)
        => new(id, boardId, size, name ?? string.Empty, extension, path ?? string.Empty);

    /// <summary>Creates a new attached-file entity after validating required fields.</summary>
    public static ErrorOr<BoardAttachedFile> Create(
        long boardId,
        long size,
        string? name,
        string? extension,
        string? path)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("BoardAttachedFile.NameEmpty", "File name cannot be empty.");

        if (string.IsNullOrWhiteSpace(path))
            return Error.Validation("BoardAttachedFile.PathEmpty", "File storage path cannot be empty.");

        if (size < 0)
            return Error.Validation("BoardAttachedFile.InvalidSize", "File size cannot be negative.");

        return new BoardAttachedFile(0, boardId, size, name, extension, path);
    }
}
