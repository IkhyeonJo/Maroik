namespace Maroik.Core.Contract.Dtos;

/// <summary>
/// Data transfer object used to save or update a file attached to a board post.
/// </summary>
public class BoardAttachedFileRequest
{
    /// <summary>Attached file ID (auto-incremented primary key).</summary>
    public long Id { get; set; }

    /// <summary>ID of the parent board post this file is attached to.</summary>
    public long BoardId { get; set; }

    /// <summary>File size in bytes.</summary>
    public long Size { get; set; }

    /// <summary>Original file name (e.g. "report.pdf").</summary>
    public string? Name { get; set; }

    /// <summary>File extension (e.g. ".pdf", ".jpg"). Null if none.</summary>
    public string? Extension { get; set; }

    /// <summary>Server-side storage path where the file is saved.</summary>
    public string? Path { get; set; }
}
