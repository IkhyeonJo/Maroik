using Maroik.Core.Contract.Dtos;

namespace Maroik.Core.Contract.Interfaces;

/// <summary>
/// Service interface for board (forum / private note) business logic,
/// including post lifecycle, comments, file attachments, and Summernote image uploads.
/// </summary>
public interface IBoardService
{
    /// <summary>Returns all visible posts of the specified board type.</summary>
    Task<IEnumerable<BoardResponse>> GetBoardsAsync(string type);

    /// <summary>Returns a single post by ID.</summary>
    Task<BoardResponse?> GetBoardByIdAsync(long id);

    /// <summary>Returns the attached file metadata for the given board post, or null if none.</summary>
    Task<BoardAttachedFileResponse?> GetAttachedFileByBoardIdAsync(long boardId);

    /// <summary>
    /// Batch-loads attached file metadata for multiple board IDs in a single query.
    /// Returns a dictionary keyed by board ID; value is null when a post has no attachment.
    /// </summary>
    Task<Dictionary<long, BoardAttachedFileResponse?>> GetAttachedFilesForBoardsAsync(IEnumerable<long> boardIds);

    /// <summary>Returns all non-deleted comments for the given board post.</summary>
    Task<IEnumerable<BoardCommentResponse>> GetCommentsByBoardIdAsync(long boardId);

    /// <summary>
    /// Batch-loads comment counts for multiple board IDs.
    /// Used to render count badges on the board list without loading comment bodies.
    /// </summary>
    Task<Dictionary<long, int>> GetCommentCountsForBoardsAsync(IEnumerable<long> boardIds);

    /// <summary>Creates a new board post and optionally saves an attached file.</summary>
    Task<ServiceResult> WriteBoardAsync(BoardRequest request, AttachedFileInfo? attachedFile);

    /// <summary>
    /// Edits an existing post. Only the original author (matched by <paramref name="writerNickname"/>) may edit.
    /// Replaces the attached file if <paramref name="newFile"/> is provided.
    /// </summary>
    Task<ServiceResult> EditBoardAsync(BoardRequest request, string writerNickname, AttachedFileInfo? newFile);

    /// <summary>
    /// Soft-deletes a board post. Admins can delete any post; regular users can only delete their own.
    /// </summary>
    Task<ServiceResult> DeleteBoardAsync(long boardId, string boardType, string requesterNickname, bool isAdmin);

    /// <summary>
    /// Adds a comment to a board post.
    /// When <paramref name="requiredOwnerNickname"/> is set, only that user may comment (used for private notes).
    /// </summary>
    Task<ServiceResult> WriteCommentAsync(BoardCommentRequest request, string? requiredOwnerNickname = null);

    /// <summary>Soft-deletes a comment. Admins can delete any comment; regular users can only delete their own.</summary>
    Task<ServiceResult> DeleteCommentAsync(long commentId, string requesterNickname, bool isAdmin);

    /// <summary>Increments the view counter of a board post by 1.</summary>
    Task<ServiceResult> IncrementBoardViewAsync(long boardId);

    /// <summary>
    /// Validates and stores an image uploaded from the Summernote editor.
    /// Returns the stored file data so the editor can render an inline preview.
    /// </summary>
    Task<SummernoteUploadResult> UploadSummernoteImageAsync(AttachedFileInfo file, string area, string boardType);

    /// <summary>Downloads the raw bytes of a stored file by its server-side path.</summary>
    Task<byte[]?> DownloadFileAsync(string filePath);

    /// <summary>
    /// Prepares HTML content for display by downloading each inline Summernote image,
    /// embedding it as Base64 in <c>data-file</c> / <c>data-contenttype</c> attributes,
    /// and replacing the stored file path in <c>alt</c> with its RSA-encrypted form.
    /// Returns the transformed HTML and a flag indicating whether any images were present.
    /// </summary>
    Task<(string Html, bool HasImages)> PrepareHtmlForDisplayAsync(string html);
}
